
//*************************************************************************************
//   !!! Generated by the fmp-cli 1.33.0.  DO NOT EDIT!
//*************************************************************************************

namespace XTC.FMP.MOD.DrugFace.LIB.Unity
{
    public class MySubjectBase
    {
        /// <summary>
        /// 创建
        /// </summary>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// data["style"] = "default";
        /// model.Publish(/XTC/DrugFace/Create, data);
        /// </example>
        public const string Create = "/XTC/DrugFace/Create";

        /// <summary>
        /// 打开
        /// </summary>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// data["source"] = "file";
        /// data["uri"] = "";
        /// data["delay"] = 0f;
        /// model.Publish(/XTC/DrugFace/Open, data);
        /// </example>
        public const string Open = "/XTC/DrugFace/Open";

        /// <summary>
        /// 关闭
        /// </summary>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// data["delay"] = 0f;
        /// model.Publish(/XTC/DrugFace/Close, data);
        /// </example>
        public const string Close = "/XTC/DrugFace/Close";

        /// <summary>
        /// 销毁
        /// </summary>
        /// <example>
        /// var data = new Dictionary<string, object>();
        /// data["uid"] = "default";
        /// model.Publish(/XTC/DrugFace/Close, data);
        /// </example>
        public const string Delete = "/XTC/DrugFace/Delete";
    }
}
