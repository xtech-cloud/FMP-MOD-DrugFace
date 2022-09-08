
using XTC.FMP.LIB.MVCS;

namespace XTC.FMP.MOD.DrugFace.LIB.MVCS
{
    /// <summary>
    /// 模块入口
    /// </summary>
    public class Entry : EntryBase
    {

        /// <summary>
        /// 静态注册
        /// </summary>
        /// <remarks>
        /// 使用(NAME+"."+_id)作为uid，注册一套MVCS
        /// </remarks>
        /// <param name="_id">id</param>
        /// <param name="_logger">日志</param>
        /// <returns>错误</returns>
        public virtual Error StaticRegister(string _id, Logger? _logger)
        {
            return base.staticRegister(_id, _logger);
        }

        /// <summary>
        /// 动态注册
        /// </summary>
        /// <remarks>
        /// 使用(NAME+"."+_id)作为uid，注册一套MVCS
        /// </remarks>
        /// <param name="_id">id</param>
        /// <param name="_logger">日志</param>
        /// <returns>错误</returns>
        public virtual Error DynamicRegister(string _id, Logger _logger)
        {
            return base.dynamicRegister(_id, _logger);
        }

        /// <summary>
        /// 静态注销
        /// </summary>
        /// <remarks>
        /// 使用(NAME+"."+_id)作为uid，注销一套MVCS
        /// </remarks>
        /// <param name="_id">id</param>
        /// <param name="_logger">日志</param>
        /// <returns>错误</returns>
        public virtual Error StaticCancel(string _id, Logger _logger)
        {
            return base.staticCancel(_id, _logger);
        }

        /// <summary>
        /// 动态注销
        /// </summary>
        /// <remarks>
        /// 使用(NAME+"."+_id)作为uid，注销一套MVCS
        /// </remarks>
        /// <param name="_id">id</param>
        /// <param name="_logger">日志</param>
        /// <returns>错误</returns>
        public virtual Error DynamicCancel(string _id, Logger _logger)
        {
            return base.dynamicCancel(_id, _logger);
        }
    }
}

