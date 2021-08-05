using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace cx_auto_sign
{
    public static class Cxim
    {
        private static string Pack(byte[] data)
        {
            return new StringBuilder()
                .Append("[\"")
                .Append(Convert.ToBase64String(data))
                .Append("\"]")
                .ToString();
        }

        public static string BuildLoginPackage(string uid, string imToken)
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            using var s = new MemoryStream();
            using var bw = new BinaryWriter(s);
            bw.Write(new byte[] { 0x08, 0x00, 0x12 });
            bw.Write((byte)(52 + uid.Length));   // 接下来到 webim_{timestamp} 的内容长度
            bw.Write(new byte[] { 0x0a, 0x0e });
            bw.Write(Encoding.ASCII.GetBytes("cx-dev#cxstudy"));
            bw.Write(new byte[] { 0x12 });
            bw.Write((byte)uid.Length);
            bw.Write(Encoding.ASCII.GetBytes(uid));
            bw.Write(new byte[] { 0x1a, 0x0b });
            bw.Write(Encoding.ASCII.GetBytes("easemob.com"));
            bw.Write(new byte[] { 0x22, 0x13 });
            bw.Write(Encoding.ASCII.GetBytes($"webim_{timestamp}"));
            bw.Write(new byte[] { 0x1a, 0x85, 0x01 });
            bw.Write(Encoding.ASCII.GetBytes("$t$"));
            bw.Write(Encoding.ASCII.GetBytes($"{imToken}"));
            bw.Write(new byte[] { 0x40, 0x03, 0x4a, 0xc0, 0x01, 0x08, 0x10, 0x12, 0x05, 0x33, 0x2e, 0x30, 0x2e, 0x30, 0x28, 0x00, 0x30, 0x00, 0x4a, 0x0d });
            bw.Write(Encoding.ASCII.GetBytes($"{timestamp}"));
            bw.Write(new byte[] { 0x62, 0x05, 0x77, 0x65, 0x62, 0x69, 0x6d, 0x6a, 0x13, 0x77, 0x65, 0x62, 0x69, 0x6d, 0x5f });
            bw.Write(Encoding.ASCII.GetBytes($"{timestamp}"));
            bw.Write(new byte[] { 0x72, 0x85, 0x01, 0x24, 0x74, 0x24 });
            bw.Write(Encoding.ASCII.GetBytes($"{imToken}"));
            bw.Write(new byte[] { 0x50, 0x00, 0x58, 0x00 });
            return Pack(s.ToArray());
        }

        public static string GetChatId(byte[] bytes)
        {
            var b = new byte[1];
            Array.Copy(bytes, 9, b, 0, 1);
            var len = Convert.ToUInt32(b[0]);
            var id = new byte[len];
            Array.Copy(bytes, 10, id, 0, len);
            return Encoding.UTF8.GetString(id);
        }
    }
}
