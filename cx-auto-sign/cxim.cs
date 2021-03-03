using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace cx_auto_sign
{
    public static class cxim
    {
        public static string BuildLoginPackage(string TUid, string ImToken)
        {
            var timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            MemoryStream s = new MemoryStream();
            var bw = new BinaryWriter(s);
            bw.Write(new byte[] { 0x08, 0x00, 0x12 });
            bw.Write((byte)(52 + TUid.Length));   // 接下来到 webim_{timestamp} 的内容长度
            bw.Write(new byte[] { 0x0a, 0x0e });
            bw.Write(Encoding.ASCII.GetBytes("cx-dev#cxstudy"));
            bw.Write(new byte[] { 0x12 });
            bw.Write((byte)TUid.Length);
            bw.Write(Encoding.ASCII.GetBytes(TUid));
            bw.Write(new byte[] { 0x1a, 0x0b });
            bw.Write(Encoding.ASCII.GetBytes("easemob.com"));
            bw.Write(new byte[] { 0x22, 0x13 });
            bw.Write(Encoding.ASCII.GetBytes($"webim_{timestamp}"));
            bw.Write(new byte[] { 0x1a, 0x85, 0x01 });
            bw.Write(Encoding.ASCII.GetBytes("$t$"));
            bw.Write(Encoding.ASCII.GetBytes($"{ImToken}"));
            bw.Write(new byte[] { 0x40, 0x03, 0x4a, 0xc0, 0x01, 0x08, 0x10, 0x12, 0x05, 0x33, 0x2e, 0x30, 0x2e, 0x30, 0x28, 0x00, 0x30, 0x00, 0x4a, 0x0d });
            bw.Write(Encoding.ASCII.GetBytes($"{timestamp}"));
            bw.Write(new byte[] { 0x62, 0x05, 0x77, 0x65, 0x62, 0x69, 0x6d, 0x6a, 0x13, 0x77, 0x65, 0x62, 0x69, 0x6d, 0x5f });
            bw.Write(Encoding.ASCII.GetBytes($"{timestamp}"));
            bw.Write(new byte[] { 0x72, 0x85, 0x01, 0x24, 0x74, 0x24 });
            bw.Write(Encoding.ASCII.GetBytes($"{ImToken}"));
            bw.Write(new byte[] { 0x50, 0x00, 0x58, 0x00 });
            return $"[\"{Convert.ToBase64String(s.ToArray())}\"]";
        }
    }
}
