using System;

namespace cx_auto_sign
{
    class Program
    {
        static void Main(string[] args)
        {
            CxSignHelper cxSign = new CxSignHelper();
            cxSign.Login("search_uuid=75c16da6%2de7f0%2d44b7%2dafa7%2de3a5570ddab0; UM_distinctid=17095cc6bbe29d-027e55961b69c4-79657361-149c48-17095cc6bbf4c5; uname=631805010409; lv=1; fid=1902; _uid=78748256; uf=b2d2c93beefa90dc7494152ab9b31a23ccc4371541657510c6e08772cd4e52b75af2381ef3aec89e0e76dabc793115f5c49d67c0c30ca5047c5a963e85f11099498d4891f254bc7ace71fc6e59483dd3578142841bd53b8d265608a4abd62c4ba3badfbabbf1830f; _d=1588751784482; UID=78748256; vc=DF821174680F8A2663EDA916A8941CD4; vc2=8B61154512E08239233F90BEB9A6B3B5; vc3=WPQlZS9U46ebrM5f3yJIzYtNkRpNoH%2FbkvMvVR6KI6REmUQqNhjKUAbhDhte0ADpoP2MqKaSwEWkWwu0jd0WJgzN5hZ1M5611LjZUHhzkZ1ppTjeo57WnYp1dFDFqxLAwEuNPtiQB7v067Yzh2c83GbxyqViTfsJsALvmkEPy%2F8%3De5c4dfe7888608c978e61497d140d4a0; xxtenc=a640e4c53ef68443cbbddc9dcdbfb29a; DSSTASH_LOG=C_38-UN_200-US_78748256-T_1588751784484; tl=1; thirdRegist=0; route=b3c261e1ab7d126a72307b18c3caa9da");
            string token = cxSign.GetToken();
            Console.WriteLine(token);
        }
    }
}
