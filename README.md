# Customer.WebApi

## 1. 项目概述
基于 **.NET 8 / ASP.NET Core Web API** 的客户积分排行榜（Leaderboard）示例服务，提供：
- 初始化样例排行榜数据
- 更新客户分数（增减分）
- 按名次区间查询排行榜
- 按客户 ID 查询其名次及上下窗口
- 登录获取 JWT（用于认证示例；Token 校验代码当前为注释状态）
- 统一响应模型与请求签名校验（Filter）

---

## 2. 技术栈（Technology Stack）
### 运行时与框架
- **.NET 8 (net8.0)**
- **C# 12**
- **ASP.NET Core Web API**

### API 与文档
- **Swashbuckle.AspNetCore 10.1.4**（Swagger/OpenAPI）

### 认证与安全
- **Microsoft.AspNetCore.Authentication.JwtBearer 8.0.25**
- **System.IdentityModel.Tokens.Jwt 8.16.0**
- HMAC-SHA256 请求签名（`HMACSHA256Utils` + `AuthFilter`，Header：`timestamp/nonce/sign/appid`）

### 日志
- **NLog 6.1.1**
- **NLog.Web.AspNetCore 6.1.2**

### 序列化
- **System.Text.Json 10.0.3**

### 测试
- **MSTest 3.6.4**
- **Aspire.Hosting.Testing 9.3.1**（已引入，当前集成测试模板示例未启用）

### 部署/容器
- 项目 csproj 包含：
  - `DockerDefaultTargetOS=Linux`
  - `Microsoft.VisualStudio.Azure.Containers.Tools.Targets 1.22.1`（VS 容器工具链）

---

## 3. 主要功能（Features）
### 3.1 登录与 JWT
- 控制器：`LoginController`
- 路由：`POST /api/v1/Login/login`
- 行为：
  - 校验请求体（`[ApiController]` + `ModelState`）
  - 账号密码示例校验：`admin/password`
  - 成功返回 JWT：`Token`，有效期 **30 分钟**（`ExpiresIn=1800`）
  - JWT 配置读取：`appsettings.json` 的 `Jwt:Key/Issuer/Audience`

### 3.2 排行榜（Leaderboard）
- 控制器：`CustomerController`
- 关键点：
  - 内存数据结构维护排行榜：`ConcurrentDictionary<long, decimal>` + `SortedSet`
  - 使用 `ReaderWriterLockSlim` 控制并发读写
  - 使用 `IMemoryCache` 做缓存（测试中也覆盖了缓存场景）

#### 初始化排行榜
- 路由：`POST /initialize`
- 说明：写入样例数据（来自 `CustomerDataService.InitCustomerScoreData()`）
- 成功返回：`"Leaderboard initialized with sample data."`

#### 更新分数
- 路由：`POST /customer/{customerid}/score/{score}`
- 规则：
  - `score` 为增量，范围 **[-1000, 1000]**（超出返回 BadRequest）
  - 新客户：若加分后为正数，加入榜单
  - 老客户：分数减到 0 可能会从榜单移除（见测试覆盖）

#### 按名次区间查询
- 路由：`GET /leaderboard?start={start}&end={end}`
- 规则：名次范围非法返回 BadRequest

#### 按客户查询（含窗口）
- 路由：`GET /leaderboard/{customerid}?high={high}&low={low}`
- 说明：返回该用户名次及上下窗口（high/low）

### 3.3 过滤器（Filter）与统一响应
- `AuthFilter`：
  - 记录请求 IP、参数
  - `ModelState` 无效时返回 `BaseResponse.Error(400, ...)`
  - 包含 Token 校验逻辑（目前注释）
  - 包含签名检查逻辑（HMAC-SHA256）
- `BaseResponse`：统一响应结构（`Success/Error`）

---

## 4. 测试用例（Test Cases）
测试项目：`Customer.WebApi.Tests`（net8.0 / MSTest）

### 4.1 CustomerDataServiceTests
文件：`Customer.WebApi.Tests/Service/CustomerDataServiceTests.cs`

- `InitCustomerScoreDataTest`
  - `InitCustomerScoreData()` 返回不为 null
  - 初始化数据条数固定为 **10**
  - 包含 `CustomerId = 38819`
  - `CustomerId = 38819` 的 `Score = 92m`
  - （备注：测试中计算了 `distinctCustomerIds`，当前未断言唯一性）

### 4.2 CustomerControllerTests
文件：`Customer.WebApi.Tests/CustomerControllerTests.cs`

- `InitializeLeaderboard_ReturnsOk_WithMessage`
  - 初始化接口返回 `OkObjectResult`
  - 返回消息为 `"Leaderboard initialized with sample data."`

- `UpdateScore_WhenOutOfRange_ReturnsBadRequest`（数据驱动）
  - `delta=-1000.01`、`delta=1000.01` 返回 `BadRequestObjectResult`
  - 返回消息 `"Score out of range."`

- `UpdateScore_NewCustomer_AddsToLeaderboard_WhenScorePositive`
  - 新客户加分后 Update 返回 Ok 且分数正确
  - 随后 `GetByCustomerId` 能查询到该客户
  - 返回数据包含 `CustomerId/Score/Rank`

- `UpdateScore_ExistingCustomer_DecreasesToZero_RemovesFromLeaderboard`
  - 对样例客户 `38819(92)` 减分到 0
  - Update 返回 Ok 且结果为 0
  - 再查询返回 `NotFoundObjectResult`，消息 `"Customer not found in leaderboard."`

- `GetByRank_WhenInvalidRange_ReturnsBadRequest`（数据驱动）
  - `(start=0,end=1)`、`(start=2,end=1)` 返回 BadRequest
  - 消息 `"Invalid rank range."`

- `GetByRank_ReturnsRanks_InRequestedWindow`
  - `start=1,end=3` 返回 3 条
  - Rank 连续且按分数降序

- `GetByCustomerId_WhenNotExists_ReturnsNotFound`
  - 不存在的 customerId 返回 NotFound
  - 消息 `"Customer not found in leaderboard."`

- `GetByCustomerId_ReturnsWindowAroundCustomer_WithCorrectRanks`
  - 对样例客户 `15514665` 查询 `high=1 low=1`
  - 返回条数在 `[1,3]`
  - Rank 单调递增，窗口边界正确

### 4.3 IntegrationTest1（模板）
文件：`Customer.WebApi.Tests/IntegrationTest1.cs`
- 引入 Aspire 分布式应用测试模板示例（当前注释，尚未绑定 AppHost 项目）

---

## 5. 部署环境（Deployment Environment）
### 5.1 基础运行环境
- **.NET 8 SDK / Runtime**
- 推荐 OS：
  - Windows（本地开发 / VS 2022）
  - Linux（容器/服务器环境）

### 5.2 配置要求
- `appsettings.json` 中需配置 JWT：
  - `Jwt:Key`
  - `Jwt:Issuer`
  - `Jwt:Audience`
- 若启用签名校验（`AuthFilter`）：
  - 需要在配置中提供 `OPENAPI:{appid}` 或 `MANAGERAPI:{appid}` 的 appkey
  - 客户端需携带 Header：`timestamp`、`nonce`、`sign`、`appid`

### 5.3 Docker（可选）
项目已标注 Linux 容器目标（`DockerDefaultTargetOS=Linux`），可在 Visual Studio 中通过：
- __Add > Docker Support__ / __Container Tools__
- 使用 __Docker Compose__ 或直接构建镜像运行（具体 Dockerfile 以仓库文件为准）

---

## 6. 运行与测试（建议）
### 本地运行
- 直接启动 `Customer.WebApi`（Kestrel）
- Swagger：通常位于 `/swagger`（取决于 `Program.cs` 配置）

### 运行单元测试
- Visual Studio：__Test > Run All Tests__
- 或命令行：
  - `dotnet test`