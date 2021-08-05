using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using CxSignHelper;
using CxSignHelper.Models;
using Newtonsoft.Json.Linq;
using Serilog;

namespace cx_auto_sign
{
    public class CourseConfig: BaseConfig
    {
        private const string ImageDir = "Images";
        private const string ImageNoneId = "041ed4756ca9fdf1f9b6dde7a83f8794";

        private static readonly Hashtable ImageCache = new();

        private readonly JToken _app;
        private readonly JToken _user;
        private readonly JToken _course;

        public bool SignEnable => GetBool(nameof(SignEnable));
        public int SignDelay => GetInt(nameof(SignDelay));
        public string SignAddress => GetString(nameof(SignAddress));
        public string SignLatitude => GetString(nameof(SignLatitude));
        public string SignLongitude => GetString(nameof(SignLongitude));
        public string SignClientIp => GetString(nameof(SignClientIp));

        public static readonly JObject Default = new()
        {
            [nameof(SignEnable)] = false,
            [GetSignTypeKey(SignType.Normal)] = true,
            [GetSignTypeKey(SignType.Gesture)] = true,
            [GetSignTypeKey(SignType.Photo)] = true,
            [GetSignTypeKey(SignType.Location)] = true,
            [nameof(SignDelay)] = 0,
            [nameof(SignAddress)] = "中国",
            [nameof(SignLatitude)] = "-1",
            [nameof(SignLongitude)] = "-1",
            [nameof(SignClientIp)] = "1.1.1.1"
        };

        public CourseConfig(BaseDataConfig app, BaseDataConfig user, BaseDataConfig course)
        {
            _app = app.GetData();
            _user = user.GetData();
            _course = course.GetData();
        }

        protected override JToken Get(string key)
        {
            return Get(_course?[key]) ??
                   Get(_user?[key]) ??
                   Get(_app?[key]) ??
                   Get(Default[key]);
        }

        private static string GetSignTypeKey(SignType type)
        {
            return "Sign" + type;
        }

        public SignOptions GetSignOptions(SignType type)
        {
            var config = Get(GetSignTypeKey(type));
            if (config?.Type == JTokenType.Boolean && config.Value<bool>() == false)
            {
                return null;
            }
            return new SignOptions
            {
                Address = SignAddress,
                Latitude = SignLatitude,
                Longitude = SignLongitude,
                ClientIp = SignClientIp
            };
        }

        private static string GetImageFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(ImageDir, path!);
            }

            return Path.GetFileName(path);
        }

        private IEnumerable<string> GetImageSet()
        {
            var set = new HashSet<string>();
            var photo = Get(SignType.Photo.ToString());
            if (photo == null)
            {
                return set;
            }
            var type = photo.Type;
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (type == JTokenType.String)
            {
                GetImage(set, photo);
            }
            else if (type == JTokenType.Array)
            {
                foreach (var token in photo)
                {
                    if (token.Type == JTokenType.String)
                    {
                        GetImage(set, token);
                    }
                }
            }
            return set;
        }
        
        private static void GetImage(ISet<string> set, string path)
        {
            if (path == null)
            {
                set.Add("");
            }
            else if (File.Exists(path))
            {
                set.Add(path);
            }
            else if (Directory.Exists(path))
            {
                GetImageDir(set, path);
            }
        }
    
        private static void GetImage(ISet<string> set, JToken token)
        {
            GetImage(set, GetImageFullPath(token.Value<string>()));
        }

        private static void GetImageDir(ISet<string> set, string path)
        {
            var infos = new DirectoryInfo(path).GetFileSystemInfos();
            foreach (var info in infos)
            {
                if ((info.Attributes & FileAttributes.Directory) != 0)
                {
                    GetImageDir(set, info.FullName);
                }
                else
                {
                    set.Add(path);
                }
            }
        }

        public async Task<string> GetImageIdAsync(CxSignClient client)
        {
            var array = GetImageSet().ToArray();
            var length = array.Length;
            if (length == 0)
            {
                return ImageNoneId;
            }
            var path = array[new Random().Next(array.Length)];
            if (string.IsNullOrEmpty(path))
            {
                return ImageNoneId;
            }
            var hash = GetHash(path);
            var id = ImageCache[hash];
            if (id != null)
            {
                return (string) id;
            }
            try
            {
                id = await client.UploadImageAsync(path);
            }
            catch (Exception e)
            {
                Log.Error(e, "上传图片失败：{Path}", path);
            }
            ImageCache[hash] = id;
            return ImageNoneId;
        }

        private static string GetHash(string path)
        {
            var file = new FileStream(path, FileMode.Open);
            var bytes = new MD5CryptoServiceProvider().ComputeHash(file);
            file.Close();
            return Convert.ToHexString(bytes);
        }
    }
}