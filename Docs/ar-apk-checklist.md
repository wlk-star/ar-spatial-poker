# AR APK 打包检查清单

目标：在 ARCore 手机上跑起可联网的 AR 德州（`ARPokerLab` 场景 + Bot 房）。

## 1. Unity Editor 内准备

- [ ] 打开 `UnityClient` 工程（Unity 6.3），确认 `feat/bot-room-full-hand` 分支最新。
- [ ] 菜单 `SpatialPoker/Build AR Poker Scene`，生成 `Assets/SpatialPoker/Scenes/ARPokerLab.unity`。
  - 场景自带：AR Session、XR Origin（含 AR Camera / ARPoseDriver / ARCameraManager /
    ARCameraBackground）、ARPlaneManager（半透明平面预制件自动生成）、
    ARRaycastManager、ARAnchorManager、TrackingStateGuard。
  - 桌子初始隐藏；运行时点按检测到的平面放置，放置后平面检测自动关闭，
    桌子挂到 AR Anchor 上。Editor 里无 AR 硬件时桌子落在相机前方 1.2m，实验室逻辑不变。
- [ ] 在 Editor 按 Play 验证：桌子出现（回退位置）、HUD 右上角有服务器地址输入框。
- [ ] `Project Settings > XR Plug-in Management > Android`：勾选 **ARCore**（代码里改不了，必须手点）。
- [ ] `Project Settings > Player > Android`：包名 `com.wlkstar.arspatialpoker`（已写进
  ProjectSettings）、Min SDK 26（已改；ARCore 最低要求）、目标架构 ARM64。

## 2. 服务器（电脑端）

- [ ] 电脑和手机连**同一个 Wi-Fi**。
- [ ] `cd Server && npm start`（默认 `:8080`）。
- [ ] 查电脑局域网 IP（如 `ipconfig` / `ifconfig`，形如 `192.168.1.20`）。
- [ ] 防火墙放行 8080 入站（Windows Defender / macOS 防火墙 / Linux ufw）。

## 3. 手机端

- [ ] 安装 APK（Build Settings > Android > Build And Run，或 Build 后 adb 安装）。
- [ ] 首次启动允许**相机权限**（AR 需要）。
- [ ] 把手机对着桌面缓慢移动，看到半透明蓝色平面后**点按平面**放置牌桌。
- [ ] HUD 右上角输入 `ws://<电脑IP>:8080`（如 `ws://192.168.1.20:8080`），点 Apply。
      地址会记住（PlayerPrefs），下次不用重输。
- [ ] 看到 `Room XXX created` 即连上 Bot 房；用底部 Fold/Check/Call/Bet/All-In 操作。

## 4. 常见问题

| 现象 | 排查 |
| --- | --- |
| 点按后桌子不出现 | 平面还没检测到：继续缓慢移动手机；确认 ARCore 勾选且设备在 [ARCore 支持列表](https://developers.google.com/ar/devices) |
| HUD 显示连接失败 | 电脑 IP 是否和手机同一网段；服务器是否在跑；防火墙 8080 |
| 牌面是文字不是贴图 | `Resources/CardFaces` 贴图缺失：重新跑 Build 菜单（它会 ForceSynchronousImport） |
| 黑屏只有 HUD | 相机权限被拒绝：系统设置里打开相机权限后重进 |

## 5. 已知限制

- AR 场景暂无手部/触摸抓牌交互：放桌 + HUD 按钮操作是当前可玩闭环。
- 多人（非 Bot）房间走同样的 `CREATE_ROOM` 流程，另一台手机输入同一服务器地址即可同房。
