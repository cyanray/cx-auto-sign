# cx-auto-sign

## ⚠注意
* 由于超星学习通更新，目前无法签到二维码签到 [#23](https://github.com/cyanray/cx-auto-sign/issues/23) 。其他类型的签到正常。
* **自动签到** 功能默认是 **关闭** 的，详见「配置说明」

## 项目简介

![](https://github.com/cyanray/cx-auto-sign/workflows/.NET%20Core/badge.svg)

cx-auto-sign 是基于 .NET5 的超星学习通自动签到工具。

本项目最低程度实现超星学习通的即时通讯协议。通过超星学习通的即时通讯协议监测最新的课程活动。当指定的课程有新的消息，就检查该课程是否有新的签到任务，如果有则进行签到。该方法与轮询相比，灵敏度更高，占用资源更低。

## 项目进度

- [x] 支持 **手机号登录** 和 **学号登录** 两种登录方式
- [x] 支持 `init` 指令，用以生成配置文件
- [x] 支持 `update` 指令，更新课程信息
- [x] 实现自动签到工作流程
- [x] 支持 **WebApi** 控制自动签到启停
- [x] 支持通过 **Server 酱** 发送通知
- [x] 支持通过 **PushPlus 推送加** 发送通知
- [x] 支持签到成功后发送 **邮件通知**
- [x] 支持多账号配置


## 使用方法

### 0x00 运行环境

首先需要在 [.Net Runtime 下载页](https://dotnet.microsoft.com/download/dotnet/current/runtime) 下载并安装 **.NET5 Runtime**（提示：Run server apps下边的下载）。

然后在 [Release页面](https://github.com/cyanray/cx-auto-sign/releases) 下载 cx-auto-sign.zip，并解压到某个目录。

（你也可以在 [Actions](https://github.com/cyanray/cx-auto-sign/actions) 中找到自动编译的测试版）

### 0x01 登录并初始化配置文件

在 cx-auto-sign.dll 所在的目录执行以下命令行（Windows 和 Linux都适用）：

```shell
# 通过手机号码登录，不需要学校编码
dotnet ./cx-auto-sign.dll init -u "双引号里面填手机号" -p "双引号里面填密码" 
```

**或：**

```shell
# 通过学号登录，需要学校编码
dotnet ./cx-auto-sign.dll init -u "双引号里面填学号" -p "双引号里面填密码" -f "学校编码"
```

### 0x02 开始自动签到

在 `cx-auto-sign.dll` 所在的目录执行以下命令行:

```shell
dotnet ./cx-auto-sign.dll work
```

即可开始自动签到。

## 配置说明

配置文件均采用 **json5** 格式，方便编辑、打注释。

### 目录结构

```text
cx-auto-sign
├── Configs
│   └── Username.json5
├── Images
│   └── ...
├── AppConfig.json5
├── ...
```

* `AppConfig.json5` 文件为 **应用配置文件**。
* `Configs` 文件夹存放 **用户配置文件**。
* `Username.json5` 文件为 **用户配置文件**，其中 `Username` 指用户名。
* `Images` 文件夹存放「图片签到」要提交的图片。

执行 `init` 指令时会创建以上文件

### 优先级

`课程配置` > `用户配置` > `应用配置`

当配置中没有该属性，或值为 `null` 时会向下一级取值。

* **应用配置**：为 `AppConfig.json5` 文件
* **用户配置**：为 `Configs` 文件夹下的文件
* **课程配置**：为 **用户配置** 中 `Couses` 属性

### 应用配置

```json5
{
  // 通知，可以在 应用配置 和 用户配置 中配置
  "ServerChanKey": "", // Server 酱的 SendKey https://sct.ftqq.com/sendkey
  "PushPlusToken": "", // PushPlus 的 Token https://www.pushplus.plus/

  // 邮箱通知，可以在 应用配置 和 用户配置 中配置
  "Email": "",        // 接收通知的邮箱
  "SmtpHost": "",     // Smtp 发件主机
  "SmtpPort": 0,      // Smtp 发件端口
  "SmtpUsername": "", // Smtp 发件邮箱
  "SmtpPassword": "", // Smtp 发件密码
  "SmtpSecure": true, // Smtp 是否使用 SSL

  // 签到配置，可以在 应用配置、用户配置 和 课程配置 中配置
  "SignEnable": false,  // 允许自动，注意：默认是不会自动签到的！！！
  "SignNormal": true,   // 允许普通签到
  "SignGesture": true,  // 允许手势签到
  "SignPhoto": true,    // 允许图片签到，默认一张黑图，可在这里设置值，详见「拍照签到参数说明」
  "SignLocation": true, // 允许位置签到
  "SignDelay": 0,       // 检测到新签到活动后延迟签到的秒数（过小容易出现秒签到现象）
  "SignAddress": "中国", // 位置签到的中文名地址
  "SignLatitude": "-1", // 位置签到的纬度
  "SignLongitude": "-1",// 位置签到的经度
  "SignClientIp": "1.1.1.1", // 签到时提交的客户端 ip 地址

  // 以下为特有属性，不会被优先级覆盖
  "DefaultUsername": "", // 默认用户
}
```

### 用户配置

```json5
{
  // 以下为特有属性，不会被优先级覆盖
  "Username": "",  // 学号或手机号
  "Password": "",  // 密码
  "Fid": "",       // 学校代号，fid 为 null 时使用手机号登录
  "WebApi": false, // 是否启动 Web Api，可以指定监听规则，详见「WebApi 说明」
  "Courses": {},   // 课程配置
}
```

### 课程配置

课程信息由程序获取的，不建议修改，但可以为课程配置不一样的签到设置。

可用 `init` 初始化或 `update` 更新课程信息

```json5
{
  "ClassId": {        // 属性名为会话 Id，不建议修改
    "CourseId": "",   // 课程 Id，不建议修改
    "ClassId": "",    // 班级 Id，不建议修改
    "ChatId": "",     // 会话 Id，不建议修改
    "CourseName": "", // 课程名，不建议修改
    "ClassName": "",  // 班级名，不建议修改

    // 可在此配置签到设置
    // 例如：
    "SignEnable": true, // 默认是不会自动签到的！！！
  }
}
```

### 拍照签到参数说明

* `true` 或 `""` 等无效路径：一张黑图
* `"."`：随机使用 `Images` 文件夹下的一张图片
* `["1.png", "2.jpg"]` 数组：随机使用数组中的一张图片
* 可使用绝对路径和相对路径，相对路径是相对于 `Images` 文件夹
* 可指定文件夹，将随机使用文件夹下的一张图片，会 **遍历** 子文件夹

例如：

* `"机房/"`：随机使用 `Images/机房` 文件夹下的一张图片
* `["", "机房/", "电脑/", "老师.jpg"]`：将从黑图片、`Images/机房` 文件夹、`Images/电脑` 文件夹和 `Images/老师.jpg` 图片中随机使用一张图片


## WebApi 说明

<details>
<summary>详细</summary>

WebApi 默认监听规则是 `http://localhost:5743`，可在配置文件中修改。

若要监听全部网卡的 5743 端口，可写为：`http://*:5743`。

### 查看状态

请求：`GET` `/status`

响应：

```jsonc
{
    "username":"0000000000",    // 学号或手机号
    "cxAutoSignEnabled":true    // 是否启动自动签到，默认为 true
}
```

### 启动自动签到

请求：`GET` `/status/enable`

响应：

```jsonc
{
    "code": 0,
    "msg":"success"
}
```

### 停止自动签到

请求：`GET` `/status/disable`

响应：

```jsonc
{
    "code": 0,
    "msg":"success"
}
```

</details>

## FQA

### 1. 如何关闭自动更新检测? 
在 `cx-auto-sign.dll` 所在目录下创建一个名为 `.noupdate` 的文件。

## 声明

一切开发旨在学习，请勿用于非法用途。
