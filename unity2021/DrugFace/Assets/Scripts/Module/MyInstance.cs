

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.DrugFace.LIB.Proto;
using XTC.FMP.MOD.DrugFace.LIB.MVCS;
using System.Collections;
using UnityEngine.Timeline;

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    public class Stage
    {
        public int index;
        public Toggle tgTime;
        public Image imgBrain;
        public Image imgHeart;
        public Image imgLiver;
        public Image imgFace;
        public float angle;
    }

    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        private ToggleGroup groupYears_;
        private Transform dial_;
        private Button btnPhoto_;
        private Button btnReset_;
        private Transform organ_;
        private Transform defaultFace_;
        private List<Stage> stages_ = new List<Stage>();
        private int currentStage_ = 0;
        private bool autoPlaying_;

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
            groupYears_ = rootUI.transform.Find("scale/years").GetComponent<ToggleGroup>();
            btnReset_ = rootUI.transform.Find("btnReset").GetComponent<Button>();
            btnPhoto_ = rootUI.transform.Find("btnPhoto").GetComponent<Button>();
            defaultFace_ = rootUI.transform.Find("face/default");

            btnReset_.onClick.AddListener(reset);
            btnPhoto_.onClick.AddListener(capture);

            for (int i = 0; i < 8; i++)
            {
                Stage stage = new Stage();
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
        }

        /// <summary>
        /// 当被删除时
        /// </summary>
        public void HandleDeleted()
        {
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
            dial_.localRotation = Quaternion.Euler(0f, 0f, 0f);
            btnPhoto_.gameObject.SetActive(true);
            btnReset_.gameObject.SetActive(false);
            defaultFace_.gameObject.SetActive(true);
            organ_.gameObject.SetActive(false);
            groupYears_.allowSwitchOff = true;
            groupYears_.SetAllTogglesOff();

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
        }

        private void capture()
        {
            btnPhoto_.gameObject.SetActive(false);
            btnReset_.gameObject.SetActive(true);
            organ_.gameObject.SetActive(true);
            stages_[0].tgTime.isOn = true;
            groupYears_.allowSwitchOff = false;
            defaultFace_.gameObject.SetActive(false);

            mono_.StartCoroutine(autoPlay());
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
    }
}
