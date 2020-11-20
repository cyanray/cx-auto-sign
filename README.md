# 项目简介

![](https://github.com/cyanray/cx-auto-sign/workflows/.NET%20Core/badge.svg)

cx-auto-sign 是基于 dotnet core 的超星学习通自动签到工具。
本项目支持以下两种监听新签到任务事件的方式：

1. 轮询各个课程的活动任务页，检查是否有新的签到任务
2. 通过超星学习通的即时通讯协议，如果指定课程有新的消息事件，则检查该课程是否有新的签到任务。(就是监听学习通App的课程聊天群组有没有新的消息)

**方式1** 需要以很高的频率访问超星学习通，频率低了有错过签到的风险。

**方式2** 的原理是老师的签到任务也是一条消息，因此理论上能通过即时通讯协议接收到该消息。本项目最低程度地实现了学习通的即时通讯协议。在接收到指定课程的消息后，就会检查并签到该课程新的签到任务。使用**方式2**可以做到秒签到，漏签可能性很低。

# 项目进度

- [x] 支持账号登录和学号登录两种登录方式
- [x] 支持 `init`指令，用以生成配置文件
- [x] 实现基于**方式2**的自动签到工作流程
- [x] 优化命令行的日志显示
- [x] 支持签到成功后发送邮件通知
- [ ] 实现基于**方式1**的自动签到工作流程
- [x] 新增 WebApi


# 使用方法

## 0x00 运行环境

首先需要在[.Net Core Runtime 下载页](https://dotnet.microsoft.com/download/dotnet-core/current/runtime)下载并安装 **.Net Core 3.1 Runtime** (提示：Run server apps下边的下载)。

然后在[Release页面](https://github.com/cyanray/cx-auto-sign/releases)下载 cx-auto-sign.zip，并解压到某个目录。

(你也可以在 [Actions](https://github.com/cyanray/cx-auto-sign/actions) 中找到自动编译的测试版)

## 0x01 登录并初始化配置文件

在 cx-auto-sign.dll 所在的目录执行以下命令行(Windows 和 Linux都适用):

```powershell
# 通过手机号码登录，不需要学校编码
dotnet ./cx-auto-sign.dll init -u "双引号里面填手机号" -p "双引号里面填密码" 
```

**或：**

```powershell
# 通过学号登录，需要学校编码
dotnet ./cx-auto-sign.dll init -u "双引号里面填学号" -p "双引号里面填密码" -f "学校编码"
```

以上指令会创建 **AppConfig.json** 文件、 **EmailConfig.json** 文件、 **Courses** 目录 和 **Images** 目录。

**AppConfig.json** 文件用于配置签到的一些参数。

**EmailConfig.json** 文件用于配置通知邮件的参数。

**Courses** 目录下有一系列 **.json** 文件，每个文件对应一门课程。对于不需要自动签到的课程，请删除对应的文件。

**Images** 目录中的图片会用于拍照签到，签到时会随机抽取一张图片用于签到。

## 0x02 开始自动签到

在 cx-auto-sign.dll 所在的目录执行以下命令行:

```powershell
dotnet ./cx-auto-sign.dll work
```

即可开始自动签到。

# 配置文件说明(AppConfig.json)

执行 **init** 指令时会创建该文件，其内容以及解释如下：

```jsonc
{
    "Username": "",             // 学号或手机号
    "Password": "",             // 密码
    "Fid": "",                  // 学校代号，fid为null时使用手机号登录
    "Address": "中国",           // 定位签到的中文名地址
    "Latitude": "-1",           // 定位签到的纬度
    "Longitude": "-1",          // 定位签到的经度
    "ClientIp": "1.1.1.1",      // 签到时提交的客户端ip地址
    "DelaySeconds": 10,         // 检测到新签到活动后延迟签到的秒数（过小容易出现秒签到现象）
    "EnableWebApi": true        // 是否启动 Web Api，默认关闭
}
```

# WebApi 说明

WebApi 默认地址是 **localhost:5743**，可在 **appsettings.json** 文件里修改。

## 查看状态

请求：GET /status

响应：

```jsonc
{
    "username":"0000000000",    // 学号或手机号
    "cxAutoSignEnabled":true    // 是否启动自动签到，默认为 true
}
```

## 启动自动签到

请求：GET /status/enable

响应：

```jsonc
{
    "code": 0,
    "msg":"success"
}
```

## 停止自动签到

请求：GET /status/disable

响应：

```jsonc
{
    "code": 0,
    "msg":"success"
}
```


# FQA

## 1. 如何关闭自动更新检测? 
在 **cx-auto-sign.dll** 所在目录下创建一个名为 **.noupdate** 的文件。

# 声明

一切开发旨在学习，请勿用于非法用途。
