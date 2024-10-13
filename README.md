# Git

## 初始化

**初始化仓库**

```c#
using(var git = new GitRepository(repoPath)){
	git.Initialize();
}
```

**克隆仓库**

```c#
using(var git = new GitRepository(repoPath)){
	string repoUrl="git@xxx.com/xxx.git"
	git.Clone(repoUrl);
}
```

## 分支

### 创建分支

```c#
git.CreateBranch(branchName);
```

--format= "%h %as %s %an"
%h: 7 位提交短哈希
%H: 提交哈希
%as: 日期格式: 2024-03-22
%s: 首行提交消息
%an: 作者名称，不包含邮件
%b: 消息正文
%ae: 邮件地址

%ad: 作者日期

%ar: 作者相对日期

%cn：提交者名称

%ce: 提交者邮件

%cd: 提交日期

%cr: 提交相对日期