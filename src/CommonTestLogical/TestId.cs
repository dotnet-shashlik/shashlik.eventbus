using System;

namespace CommonTestLogical
{
    public static class CurrentTestIdClass
    {
        /// <summary>
        /// 每次测试id号，避免旧的消息队列里面的消息消费影响测试数据
        /// </summary>
        public static readonly string TestIdNo = Guid.NewGuid().ToString("n");
    }
}