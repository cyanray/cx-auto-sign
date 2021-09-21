using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static readonly string[] ImageSuffixes = { "png", "jpg", "jpeg", "bmp", "gif", "webp" };

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
            _course = course?.GetData();
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
            return Path.GetFullPath(path);
        }

        private IEnumerable<string> GetImageSet()
        {
            var set = new HashSet<string>();
            var photo = Get(GetSignTypeKey(SignType.Photo));
            // ReSharper disable once InvertIf
            if (photo != null)
            {
                var type = photo.Type;
                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (type == JTokenType.String)
                {
                    AddToImageSet(set, photo);
                }
                else if (type == JTokenType.Array)
                {
                    foreach (var token in photo)
                    {
                        if (token.Type == JTokenType.String)
                        {
                            AddToImageSet(set, token);
                        }
                    }
                }
            }
            return set;
        }

        private static void AddFileToImageSet(ISet<string> set, string path)
        {
            var name = Path.GetFileName(path);
            var index = name.LastIndexOf('.') + 1;
            if (index == 0)
            {
                return;
            }
            var suffix = name[index..].ToLower();
            if (!ImageSuffixes.Contains(suffix))
            {
                return;
            }
            set.Add(path);
        }

        private static void AddToImageSet(ISet<string> set, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                set.Add("");
                return;
            }
            if (File.Exists(path))
            {
                AddFileToImageSet(set, path);
            }
            else if (Directory.Exists(path))
            {
                AddDirToImageSet(set, path);
            }
        }

        private static void AddToImageSet(ISet<string> set, JToken token)
        {
            AddToImageSet(set, GetImageFullPath(token.Value<string>()));
        }

        private static void AddDirToImageSet(ISet<string> set, string path)
        {
            var infos = new DirectoryInfo(path).GetFileSystemInfos();
            foreach (var info in infos)
            {
                var name = info.FullName;
                if ((info.Attributes & FileAttributes.Directory) != 0)
                {
                    AddDirToImageSet(set, name);
                }
                else
                {
                    AddFileToImageSet(set, name);
                }
            }
        }

        public async Task<string> GetImageIdAsync(CxSignClient client, ILogger log)
        {
            var array = GetImageSet().ToArray();
            var length = array.Length;
            if (length != 0)
            {
                log.Information("将从这些图片中随机选择一张进行图片签到：{Array}", array);
                var path = array[new Random().Next(length)];
                if (!string.IsNullOrEmpty(path))
                {
                    log.Information("将使用这张照片进行图片签到：{Path}", path);
                    try
                    {
                        return await client.UploadImageAsync(path);
                    }
                    catch (Exception e)
                    {
                        log.Error(e, "上传图片失败");
                    }
                }
            }
            log.Information("将使用一张黑图进行图片签到");
            return ImageNoneId;
        }
    }
}