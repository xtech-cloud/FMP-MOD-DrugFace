

using System.Collections.Generic;
using System.Threading.Tasks;
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
using static System.Net.WebRequestMethods;
using System.IO;
using OpenCover.Framework.Model;
using System.Buffers.Text;
using OpenCVCompact;

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
            Idle,
            Ready,
            Busy
        }

        private ToggleGroup groupYears_;
        private Transform dial_;
        private Button btnPhoto_;
        private Button btnReset_;
        private Transform organ_;
        private Transform defaultFace_;
        private Image scaleLight_;
        private RawImage imgCamera_;
        private List<Stage> stages_ = new List<Stage>();
        private Image[] numbers_ = new Image[6];
        private int currentStage_ = 0;
        private bool autoPlaying_;
        private Coroutine coroutineDetectFace_;
        private Coroutine coroutineInvokeAPI_;
        private Status status_;

        private CameraFaceAnalyzer faceAnalyzer_;

        public MyInstance(string _uid, string _style, MyConfig _config, LibMVCS.Logger _logger, Dictionary<string, LibMVCS.Any> _settings, MyEntryBase _entry, MonoBehaviour _mono, GameObject _rootAttachments)
            : base(_uid, _style, _config, _logger, _settings, _entry, _mono, _rootAttachments)
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
            imgCamera_ = rootUI.transform.Find("face/camera").GetComponent<RawImage>();
            numbers_[0] = rootUI.transform.Find("face/camera/5").GetComponent<Image>();
            numbers_[1] = rootUI.transform.Find("face/camera/4").GetComponent<Image>();
            numbers_[2] = rootUI.transform.Find("face/camera/3").GetComponent<Image>();
            numbers_[3] = rootUI.transform.Find("face/camera/2").GetComponent<Image>();
            numbers_[4] = rootUI.transform.Find("face/camera/1").GetComponent<Image>();
            numbers_[5] = rootUI.transform.Find("face/camera/0").GetComponent<Image>();

            btnReset_.onClick.AddListener(reset);
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
                    autoPlaying_ = false;
                    stage.tgTime.isOn = true;
                });
                stage.tgTime.onValueChanged.AddListener((_toggle) =>
                {
                    if (!_toggle)
                        return;
                    mono_.StartCoroutine(rotateDial(stage));
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
                stage.tgTime.interactable = false;
            }

            currentStage_ = -1;
            status_ = Status.Idle;
        }

        private void capture()
        {
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
                btnReset_.gameObject.SetActive(true);
                organ_.gameObject.SetActive(true);
                stages_[0].tgTime.isOn = true;
                groupYears_.allowSwitchOff = false;
                defaultFace_.gameObject.SetActive(false);
                imgCamera_.gameObject.SetActive(false);

                mono_.StartCoroutine(autoPlay());
            }));
        }

        private IEnumerator rotateDial(Stage _stage)
        {
            // 动画前关闭交互
            foreach (var stage in stages_)
            {
                stage.tgTime.interactable = false;
            }

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

            // 动画后打开交互
            foreach (var stage in stages_)
            {
                stage.tgTime.interactable = true;
            }
        }

        private IEnumerator autoPlay()
        {
            int index = 1;
            autoPlaying_ = true;
            while (true)
            {
                yield return new WaitForSeconds(3);
                if (!autoPlaying_)
                    break;
                var stage = stages_[index];
                stage.tgTime.isOn = true;
                index += 1;
                if (index >= stages_.Count)
                    break;
            }
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
                // 没有检测到人脸，重置计时器
                if (!faceDetected)
                {
                    numberIndex = 0;
                    reset();
                    continue;
                }

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

                status_ = Status.Ready;
            }
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
            }

            public string version = "4.0";
            public float alpha = 0.5f;
            public Image image_template = new Image();
            public Image image_target = new Image();
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
            var webCamTexture = faceAnalyzer_.WebCamTexture;
            logger_.Info("size of webcam is {0}x{1}", webCamTexture.width, webCamTexture.height);
            Texture2D t2d = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
            t2d.SetPixels(webCamTexture.GetPixels()); 
            t2d.Apply();

            defaultFace_.gameObject.SetActive(true);
            byte[] imageTempleteData = t2d.EncodeToJPG();
            logger_.Info("size of image_template is {0}", imageTempleteData.Length);
            string imageTemplateBase64 = System.Convert.ToBase64String(imageTempleteData);

            // 载入目标图
            // {
            string datapath = settings_["datapath"].AsString();
            string vendor = settings_["vendor"].AsString();
            string dir = System.IO.Path.Combine(datapath, vendor);
            dir = System.IO.Path.Combine(dir, "themes");
            dir = System.IO.Path.Combine(dir, MyEntryBase.ModuleName);
            string targetImageFile = System.IO.Path.Combine(dir, "4/m_1.jpg");
            string targetBase64 = "";
            logger_.Trace(targetImageFile);
            using (var uwr = new UnityWebRequest(new Uri(targetImageFile)))
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
                logger_.Info("size of image_target is {0}", data.Length);
                targetBase64 = System.Convert.ToBase64String(data);
            }
            // }

            // 获取token
            // {
            WWWForm form = new WWWForm();
            form.AddField("grant_type", "client_credentials");
            form.AddField("client_id", "fZqIE9XOwBujIVyiB1OpiZ3h");
            form.AddField("client_secret", "a64mqWt4Hbkz6FrmeykwaAceaRqCO3SO");

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

            string url = "https://aip.baidubce.com/rest/2.0/face/v1/merge?access_token=" + token;
            var request = new MergeRequest();
            request.image_template.image = imageTemplateBase64;
            request.image_target.image = targetBase64;
            string json = JsonConvert.SerializeObject(request);
            logger_.Info(json);

            // 最后一个阶段不用生成
            for (int i = 0; i < stages_.Count - 1; i++)
            {
                request.alpha = (float)Math.Round(stages_[i].year / 15.0f, 2);
                logger_.Trace(request.alpha.ToString());
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
                    System.IO.File.WriteAllBytes(string.Format("D:/{0}.jpg", i), data);
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
            yield return new WaitForEndOfFrame();
            _onSuccess();
        }
    }
}
