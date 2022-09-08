
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.MOD.DrugFace.LIB.MVCS
{
    public class HealthyFacade : View.Facade
    {
        /// <summary>
        /// 完整名称
        /// </summary>
        public const string NAME = "XTC.FMP.MOD.DrugFace.LIB.MVCS.HealthyFacade";

        /// <summary>
        /// 带uid参数的构造函数
        /// </summary>
        /// <param name="_uid">实例化后的唯一识别码</param>
        public HealthyFacade(string _uid, string _gid) : base(_uid)
        {
            gid_ = _gid;
        }

        /// <summary>
        /// 直系的MVCS的四个组件的组的ID
        /// </summary>
        protected string gid_ = "";
    }
}

