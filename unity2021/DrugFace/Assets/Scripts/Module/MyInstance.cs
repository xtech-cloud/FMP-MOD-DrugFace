

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.DrugFace.LIB.Proto;
using XTC.FMP.MOD.DrugFace.LIB.MVCS;
using System.Collections;
using FaceAnalyzer;
using UnityEngine.Networking;
using System;
using Newtonsoft.Json;
using System.IO;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    public class Stage
    {
        public int index;
        public int year;
        public Toggle tgTime;
        public Image imgBrain;
        public Image imgHeart;
        public Image imgLiver;
        public Image imgFace;
        public float angle;
        public float fillAmount;
    }


    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        enum Status
        {
            Idle, // 空闲
            Ready, // 倒计时
            Busy // 图像合成和图像展示中
        }
        public class UiReference
        {
            public class Infobox
            {
                public GameObject male;
                public GameObject female;
                public GameObject glassY;
                public GameObject glassN;
                public GameObject hatY;
                public GameObject hatN;
                public Text age;
            }
            public Text textIdleTimer;
            public Infobox infobox = new Infobox();
        }

        private ToggleGroup groupYears_;
        private Transform dial_;
        private Button btnPhoto_;
        private Button btnReset_;
        private Transform organ_;
        private Transform defaultFace_;
        private GameObject imgMerging_;
        private Image scaleLight_;
        private RawImage imgCamera_;
        private List<Stage> stages_ = new List<Stage>();
        private Image[] numbers_ = new Image[6];
        private int currentStage_ = 0;
        private Coroutine coroutineDetectFace_;
        private Coroutine coroutineInvokeAPI_;
        private Status status_;
        private UiReference uiReference_ = new UiReference();

        private CameraFaceAnalyzer faceAnalyzer_;
        private int idleTimer_ = 0;
        private Coroutine coroutineIdleTick_ = null;
        private Coroutine coroutineAutoplay_ = null;
        private Coroutine coroutineRotateDial_ = null;

        public MyInstance(string _uid, string _style, MyConfig _config, MyCatalog _catalog, LibMVCS.Logger _logger, Dictionary<string, LibMVCS.Any> _settings, MyEntryBase _entry, MonoBehaviour _mono, GameObject _rootAttachments)
            : base(_uid, _style, _config, _catalog, _logger, _settings, _entry, _mono, _rootAttachments)
        {
        }

        /// <summary>
        /// 当被创建时
        /// </summary>
        public void HandleCreated()
        {
            stages_.Clear();

            dial_ = rootUI.transform.Find("dial");
            organ_ = rootUI.transform.Find("organ");
            scaleLight_ = rootUI.transform.Find("scale/light").GetComponent<Image>();
            groupYears_ = rootUI.transform.Find("scale/years").GetComponent<ToggleGroup>();
            btnReset_ = rootUI.transform.Find("btnReset").GetComponent<Button>();
            btnPhoto_ = rootUI.transform.Find("btnPhoto").GetComponent<Button>();
            defaultFace_ = rootUI.transform.Find("face/default");
            imgMerging_ = rootUI.transform.Find("face/merging").gameObject;
            imgCamera_ = rootUI.transform.Find("face/camera").GetComponent<RawImage>();
            numbers_[0] = rootUI.transform.Find("face/camera/5").GetComponent<Image>();
            numbers_[1] = rootUI.transform.Find("face/camera/4").GetComponent<Image>();
            numbers_[2] = rootUI.transform.Find("face/camera/3").GetComponent<Image>();
            numbers_[3] = rootUI.transform.Find("face/camera/2").GetComponent<Image>();
            numbers_[4] = rootUI.transform.Find("face/camera/1").GetComponent<Image>();
            numbers_[5] = rootUI.transform.Find("face/camera/0").GetComponent<Image>();
            uiReference_.infobox.male = rootUI.transform.Find("infobox/gender/value_m").gameObject;
            uiReference_.infobox.female = rootUI.transform.Find("infobox/gender/value_f").gameObject;
            uiReference_.infobox.glassY = rootUI.transform.Find("infobox/glass/value_y").gameObject;
            uiReference_.infobox.glassN = rootUI.transform.Find("infobox/glass/value_n").gameObject;
            uiReference_.infobox.hatY = rootUI.transform.Find("infobox/hat/value_y").gameObject;
            uiReference_.infobox.hatN = rootUI.transform.Find("infobox/hat/value_n").gameObject;
            uiReference_.infobox.age = rootUI.transform.Find("infobox/age/value").GetComponent<Text>();
            uiReference_.textIdleTimer = rootUI.transform.Find("textIdleTimer").GetComponent<Text>();

            btnReset_.onClick.AddListener(() =>
            {
                if (null != coroutineIdleTick_)
                {
                    mono_.StopCoroutine(coroutineIdleTick_);
                    coroutineIdleTick_ = null;
                }
                reset();
            });
            btnPhoto_.onClick.AddListener(capture);

            int[] years = new int[] { 1, 2, 4, 6, 8, 10, 12, 15 };
            float[] fillAmounts = new float[] { 0.03f, 0.17f, 0.3f, 0.444f, 0.58f, 0.724f, 0.858f, 1 };
            for (int i = 0; i < 8; i++)
            {
                Stage stage = new Stage();
                stage.year = years[i];
                stage.fillAmount = fillAmounts[i];
                stage.index = i;
                stage.tgTime = rootUI.transform.Find("scale/years").GetChild(i).GetComponent<Toggle>();
                stage.imgBrain = rootUI.transform.Find("organ/1").GetChild(i).GetComponent<Image>();
                stage.imgHeart = rootUI.transform.Find("organ/2").GetChild(i).GetComponent<Image>();
                stage.imgLiver = rootUI.transform.Find("organ/3").GetChild(i).GetComponent<Image>();
                stage.imgFace = rootUI.transform.Find("face").GetChild(i).GetComponent<Image>();
                // 从-90度到-270的角度插值计算，会变成-90 -> 0 -> -270，而需要的结果是-90 -> -180 -> -270
                // 所以将区间改为(-91,-269)避开这个问题
                stage.angle = -91 - i * 178 / 7;
                stage.tgTime.transform.Find("button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    stage.tgTime.isOn = true;
                    resetIdleTimer();
                });
                stage.tgTime.onValueChanged.AddListener((_toggle) =>
                {
                    if (!_toggle)
                        return;
                    if (null != coroutineRotateDial_)
                        mono_.StopCoroutine(coroutineRotateDial_);
                    coroutineRotateDial_ = mono_.StartCoroutine(rotateDial(stage));
                });
                stages_.Add(stage);
            }


            faceAnalyzer_ = new CameraFaceAnalyzer();
            faceAnalyzer_.mono = mono_;
            faceAnalyzer_.width = style_.camera.width;
            faceAnalyzer_.height = style_.camera.height;
            faceAnalyzer_.fps = style_.camera.fps;
            faceAnalyzer_.targetImage = imgCamera_;
            faceAnalyzer_.Run();

            coroutineDetectFace_ = mono_.StartCoroutine(detectFace());
        }

        /// <summary>
        /// 当被删除时
        /// </summary>
        public void HandleDeleted()
        {
            mono_.StopCoroutine(coroutineDetectFace_);
        }

        /// <summary>
        /// 当被打开时
        /// </summary>
        public void HandleOpened(string _source, string _uri)
        {
            reset();
            rootUI.gameObject.SetActive(true);
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void HandleClosed()
        {
            rootUI.gameObject.SetActive(false);
        }

        private void reset()
        {
            status_ = Status.Idle;
            dial_.localRotation = Quaternion.Euler(0f, 0f, 0f);
            btnPhoto_.gameObject.SetActive(true);
            btnReset_.gameObject.SetActive(false);
            defaultFace_.gameObject.SetActive(true);
            imgMerging_.gameObject.SetActive(false);
            imgCamera_.gameObject.SetActive(true);
            organ_.gameObject.SetActive(false);
            groupYears_.allowSwitchOff = true;
            groupYears_.SetAllTogglesOff();
            scaleLight_.fillAmount = 0f;
            foreach (var number in numbers_)
            {
                number.gameObject.SetActive(false);
            }

            Color color = Color.white;
            color.a = 0;
            foreach (var stage in stages_)
            {
                stage.imgBrain.color = color;
                stage.imgHeart.color = color;
                stage.imgLiver.color = color;
                stage.imgFace.color = color;
            }

            foreach (var stage in stages_)
            {
                stage.tgTime.transform.Find("button").GetComponent<Button>().interactable = false;
            }

            uiReference_.infobox.female.SetActive(false);
            uiReference_.infobox.male.SetActive(false);
            uiReference_.infobox.glassY.SetActive(false);
            uiReference_.infobox.glassN.SetActive(false);
            uiReference_.infobox.hatY.SetActive(false);
            uiReference_.infobox.hatN.SetActive(false);
            uiReference_.infobox.age.text = "";
            uiReference_.textIdleTimer.gameObject.SetActive(false);

            currentStage_ = -1;
        }

        private void capture()
        {
            logger_.Info(faceAnalyzer_.attrGlasses);
            logger_.Info(faceAnalyzer_.attrHat);
            if (null != faceAnalyzer_.attrGender)
            {
                uiReference_.infobox.female.SetActive(faceAnalyzer_.attrGender.StartsWith("Female"));
                uiReference_.infobox.male.SetActive(faceAnalyzer_.attrGender.StartsWith("Male"));
            }
            if (null != faceAnalyzer_.attrGlasses)
            {
                uiReference_.infobox.glassY.SetActive(faceAnalyzer_.attrGlasses == "Yes");
                uiReference_.infobox.glassN.SetActive(faceAnalyzer_.attrGlasses == "No");
            }
            if (null != faceAnalyzer_.attrHat)
            {
                uiReference_.infobox.hatY.SetActive(faceAnalyzer_.attrHat == "Yes");
                uiReference_.infobox.hatN.SetActive(faceAnalyzer_.attrHat == "No");
            }
            if (null != faceAnalyzer_.attrAge)
            {
                uiReference_.infobox.age.text = faceAnalyzer_.attrAge;
            }

            imgMerging_.gameObject.SetActive(true);
            if (null != coroutineInvokeAPI_)
                mono_.StopCoroutine(coroutineInvokeAPI_);
            coroutineInvokeAPI_ = mono_.StartCoroutine(invokeAPI(() =>
            {
                coroutineInvokeAPI_ = null;
                reset();
            }, () =>
            {
                coroutineInvokeAPI_ = null;

                btnPhoto_.gameObject.SetActive(false);
                organ_.gameObject.SetActive(true);
                groupYears_.allowSwitchOff = false;
                defaultFace_.gameObject.SetActive(false);
                imgMerging_.SetActive(false);
                imgCamera_.gameObject.SetActive(false);

                if (null != coroutineAutoplay_)
                    mono_.StopCoroutine(coroutineAutoplay_);
                coroutineAutoplay_ = mono_.StartCoroutine(autoPlay());
            }));
        }

        private IEnumerator rotateDial(Stage _stage)
        {
            logger_.Debug("rotate dial started");
            Stage stageOut = null;
            if (currentStage_ >= 0)
            {
                stageOut = stages_[currentStage_];
            }

            float timer = 0f;
            float startAngle = dial_.localRotation.eulerAngles.z;
            float endAngle = _stage.angle;
            float duration = 0.3f;
            Color colorIn = Color.white;
            Color colorOut = Color.white;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
                colorIn.a = timer / duration;
                colorOut.a = 1 - timer / duration;
                dial_.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(startAngle, endAngle, timer / duration));
                _stage.imgBrain.color = colorIn;
                _stage.imgHeart.color = colorIn;
                _stage.imgLiver.color = colorIn;
                _stage.imgFace.color = colorIn;
                if (null != stageOut)
                {
                    stageOut.imgBrain.color = colorOut;
                    stageOut.imgHeart.color = colorOut;
                    stageOut.imgLiver.color = colorOut;
                    stageOut.imgFace.color = colorOut;
                }
            }
            dial_.localRotation = Quaternion.Euler(0, 0, endAngle);
            colorIn.a = 1;
            colorOut.a = 0;
            _stage.imgBrain.color = colorIn;
            _stage.imgHeart.color = colorIn;
            _stage.imgLiver.color = colorIn;
            _stage.imgFace.color = colorIn;
            if (null != stageOut)
            {
                stageOut.imgBrain.color = colorOut;
                stageOut.imgHeart.color = colorOut;
                stageOut.imgLiver.color = colorOut;
                stageOut.imgFace.color = colorOut;
            }
            currentStage_ = _stage.index;

            logger_.Debug("rotate dial finished");
            // 自动播放完成
            coroutineRotateDial_ = null;
            if (currentStage_ >= stages_.Count - 1)
                onAutoPlayFinish();
        }

        private IEnumerator autoPlay()
        {
            logger_.Debug("autoPlay Started");
            stages_[0].tgTime.isOn = true;
            int index = 1;
            while (true)
            {
                yield return new WaitForSeconds(3);
                logger_.Debug("autoPlay tick");
                var stage = stages_[index];
                stage.tgTime.isOn = true;
                index += 1;
                if (index >= stages_.Count)
                    break;
            }
            coroutineAutoplay_ = null;
            logger_.Debug("autoPlay Finished");
        }

        private void onAutoPlayFinish()
        {
            btnReset_.gameObject.SetActive(true);
            //允许手动点击
            foreach (var stage in stages_)
            {
                stage.tgTime.transform.Find("button").GetComponent<Button>().interactable = true;
            }

            //启动闲置计时器
            coroutineIdleTick_ = mono_.StartCoroutine(startIdleTimer());
        }

        private IEnumerator detectFace()
        {
            float timer = 0;
            int numberIndex = 0;
            while (true)
            {
                yield return new WaitForEndOfFrame();
                if (Status.Busy == status_)
                {
                    continue;
                }

                timer += Time.deltaTime;

                bool faceDetected = faceAnalyzer_.DetectFace();
                defaultFace_.gameObject.SetActive(!faceDetected);
                // 没有检测到人脸，重置整个状态到Idle
                // 检测同时在Ready状态也执行，并可中断Ready状态
                if (!faceDetected)
                {
                    numberIndex = 0;
                    reset();
                    continue;
                }

                // 在Ready阶段倒计时
                if (Status.Ready == status_)
                {
                    if (timer >= 1.0f)
                    {
                        for (int i = 0; i < numbers_.Length; ++i)
                        {
                            numbers_[i].gameObject.SetActive(i == numberIndex);
                        }
                        numberIndex += 1;
                        if (numberIndex == 6)
                        {
                            status_ = Status.Busy;
                            numberIndex = 0;
                            capture();
                        }
                        timer = 0.0f;
                    }
                    continue;
                }

                // 检测到人脸时在下一帧倒计时
                status_ = Status.Ready;
            }
        }

        private IEnumerator startIdleTimer()
        {
            resetIdleTimer();
            while (true)
            {
                int left = style_.idleTimer.timeout - idleTimer_;
                uiReference_.textIdleTimer.text = left.ToString();
                if (left <= style_.idleTimer.appear)
                    uiReference_.textIdleTimer.gameObject.SetActive(true);
                yield return new WaitForSeconds(1.0f);
                idleTimer_ += 1;
                if (idleTimer_ > style_.idleTimer.timeout)
                    break;
            }
            reset();
            coroutineIdleTick_ = null;
        }

        private void resetIdleTimer()
        {
            idleTimer_ = 0;
            uiReference_.textIdleTimer.gameObject.SetActive(false);
        }



        class ReplyAPI
        {
            public string access_token;
            public string error = "";
        }

        class MergeRequest
        {
            public class Image
            {
                public string image = "";
                public string image_type = "BASE64";
                public string quality_control = "HIGH";
            }

            public string version = "1.0";
            public string alpha = "0.5";
            public string merge_degree = "NORMAL";
            public Image image_template = new Image();
            public Image image_target = new Image();

            public override string ToString()
            {
                return String.Format("version:{0} merge_degree:{1} templateQuality:{2} targetQuality:{3}", version, merge_degree, image_template.quality_control, image_target.quality_control);
            }
        }

        class MergeReply
        {
            public class Result
            {
                public string merge_image;
            }
            public int error_code = 0;
            public string error_msg = "";
            public Result result = new Result();
        }

        private IEnumerator invokeAPI(Action _onError, Action _onSuccess)
        {
            // 获取token
            // {
            WWWForm form = new WWWForm();
            form.AddField("grant_type", "client_credentials");
            form.AddField("client_id", "fZqIE9XOwBujIVyiB1OpiZ3h");
            form.AddField("client_secret", "EnLC2sPOkcF3WGkZbnHpcXRwOzFL5lAv");

            string token;
            using (UnityWebRequest uwr = UnityWebRequest.Post("https://aip.baidubce.com/oauth/2.0/token", form))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    logger_.Error(uwr.error);
                    _onError();
                    yield break;
                }

                string result = uwr.downloadHandler.text;
                logger_.Info(result);
                var reply = JsonConvert.DeserializeObject<ReplyAPI>(result);
                if (null == reply)
                {
                    logger_.Error("reply is null");
                    _onError();
                    yield break;
                }

                if (string.IsNullOrEmpty(reply.access_token))
                {
                    logger_.Error(reply.error);
                    _onError();
                    yield break;
                }
                token = reply.access_token;
            }
            // }

            string imageCamBase64;
            if (style_.debug.useDebugPhoto)
                imageCamBase64 = getDebugImageBase64();
            else
                imageCamBase64 = getCamImageBase64();


            string themesDir = settings_["path.themes"].AsString();
            themesDir = Path.Combine(themesDir, MyEntryBase.ModuleName);

            string url = "https://aip.baidubce.com/rest/2.0/face/v1/merge?access_token=" + token;
            var request = new MergeRequest();
            request.version = style_.mergeMatrix.version;
            request.merge_degree = style_.mergeMatrix.degree;
            request.image_template.quality_control = style_.mergeMatrix.templateQuality;
            request.image_target.quality_control = style_.mergeMatrix.targetQuality;
            logger_.Info(request.ToString());
            if (request.version == "4.0")
            {
                switch (request.merge_degree)
                {
                    case "LOW": request.alpha = "0.75"; break;
                    case "NORMAL": request.alpha = "0.5"; break;
                    case "HIGH": request.alpha = "0.25"; break;
                    case "COMPLETE": request.alpha = "0"; break;
                    default: request.alpha = "1"; break;
                }
            }

            int column = UnityEngine.Random.Range(0, style_.mergeMatrix.column);
            // 最后一个阶段不用生成
            for (int i = 0; i < stages_.Count - 1; i++)
            {
                string image = getThemeImage(i, column);
                // 载入模板图
                string imageAvatarBase64 = "";
                logger_.Trace("ready to load template image from {0}", image);
                using (var uwr = new UnityWebRequest(new Uri(image)))
                {
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    yield return uwr.SendWebRequest();
                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        logger_.Error(uwr.error);
                        _onError();
                        yield break;
                    }
                    var data = uwr.downloadHandler.data;
                    logger_.Info("size of template_image is {0}", data.Length);
                    imageAvatarBase64 = System.Convert.ToBase64String(data);
                }

                request.image_template.image = imageCamBase64;
                request.image_target.image = imageAvatarBase64;
                string json = JsonConvert.SerializeObject(request);
                using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
                {
                    byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(json);
                    uwr.uploadHandler = new UploadHandlerRaw(postBytes);
                    uwr.downloadHandler = new DownloadHandlerBuffer();
                    uwr.SetRequestHeader("Content-Type", "application/json");
                    yield return uwr.SendWebRequest();

                    if (uwr.result != UnityWebRequest.Result.Success)
                    {
                        logger_.Error(uwr.error);
                        _onError();
                        yield break;
                    }

                    string result = uwr.downloadHandler.text;
                    var reply = JsonConvert.DeserializeObject<MergeReply>(result);
                    if (null == reply)
                    {
                        logger_.Error("reply is null");
                        _onError();
                        yield break;
                    }

                    if (0 != reply.error_code)
                    {
                        logger_.Error(reply.error_msg);
                        _onError();
                        yield break;
                    }
                    byte[] data = System.Convert.FromBase64String(reply.result.merge_image);
                    if (style_.debug.saveResultImage)
                    {
                        saveImage(string.Format("_merge{0}.jpg", i + 1), data);
                    }
                    Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    texture.LoadImage(data);
                    Sprite sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    stages_[i].imgFace.sprite = sprite;
                    scaleLight_.fillAmount = stages_[i].fillAmount;
                }
            }
            logger_.Info("merge success!!");
            scaleLight_.fillAmount = 1.0f;
            defaultFace_.gameObject.SetActive(false);
            imgMerging_.SetActive(false);
            yield return new WaitForEndOfFrame();
            _onSuccess();
        }

        private string getCamImageBase64()
        {
            var webCamTexture = faceAnalyzer_.WebCamTexture;
            logger_.Info("size of webcam is {0}x{1}", webCamTexture.width, webCamTexture.height);
            // TODO 适应小边，目前是假设高度比宽度小
            int minSize = webCamTexture.height;
            int maxSize = webCamTexture.width;
            var colors = webCamTexture.GetPixels((maxSize - minSize) / 2, 0, minSize, minSize);
            Texture2D t2d = new Texture2D(minSize, minSize, TextureFormat.RGBA32, false);
            t2d.SetPixels(colors);
            t2d.Apply();

            byte[] imageData = t2d.EncodeToJPG();
            logger_.Info("size of image_template is {0}", imageData.Length);
            saveImage(String.Format("_{0}.jpg", System.DateTime.Now.ToString("yyyyMMddhhmmss")), imageData);
            string imageBase64 = System.Convert.ToBase64String(imageData);
            return imageBase64;
        }

        private string getDebugImageBase64()
        {
            string themesDir = settings_["path.themes"].AsString();
            themesDir = Path.Combine(themesDir, MyEntryBase.ModuleName);
            themesDir = Path.Combine(themesDir, "_debug");
            string file = Path.Combine(themesDir, "debug.jpg");
            byte[] data = File.ReadAllBytes(file);
            return Convert.ToBase64String(data);
        }

        private string getThemeImage(int _row, int _column)
        {
            if (_row >= style_.mergeMatrix.row)
            {
                return null;
            }

            string gender = faceAnalyzer_.attrGender;
            string image;
            if (gender == "female")
            {
                image = style_.mergeMatrix.rowS[_row].femaleImageS[_column].file;
            }
            else
            {
                image = style_.mergeMatrix.rowS[_row].maleImageS[_column].file;
            }

            string themesDir = settings_["path.themes"].AsString();
            themesDir = Path.Combine(themesDir, MyEntryBase.ModuleName);
            string file = Path.Combine(themesDir, image);
            return file;
        }

        private void saveImage(string _name, byte[] _data)
        {
            string debugDir = settings_["path.themes"].AsString();
            debugDir = Path.Combine(debugDir, MyEntryBase.ModuleName);
            debugDir = Path.Combine(debugDir, "_debug");
            if (!Directory.Exists(debugDir))
            {
                Directory.CreateDirectory(debugDir);
            }

            string file = Path.Combine(debugDir, _name);
            try
            {
                File.WriteAllBytes(file, _data);
            }
            catch (Exception e)
            {
                logger_.Exception(e);
            }
        }
    }
}
