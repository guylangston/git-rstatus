# git-rstatus

> Simple, fast, recursive scan for all git repos. Async `git fetch && git status`

`git-rstatus` is a quality-of-life tool to quickly see that status of numerous
project. It is a small/simple help unlike the larger fully-feature `lazygit`.

![Screenshot](./doc/git-rstatus-0.4.0.gif)

## TLDR; Quick start

```bash
# find all git projects and display `status`
# will recursively search user user ~ and /mnt/my_git_repos
git-rstatus ~ /mnt/my_git_repos

# real-world example
# (1) don't search `tmux` or `archive` folders
# (2) don't call `git fetch` on `linux` folder
# (3) `git pull` before `git status`
# (4) display absolute paths
# (5) recursive search in `~/repo` and `~/scripts/`
git-rstatus --exclude tmux,archive --no-fetch linux --pull --abs ~/repo ~/scripts


# real-world workflow: create a script with all args, and add it to the $PATH
# ───────┬────────────────────────────────────────────────────────────────────────────────────
#        │ File: /home/guy/scripts/git-rstatus-update.sh
# ───────┼────────────────────────────────────────────────────────────────────────────────────
# 1      │ #!/bin/sh
# 2      │ git-rstatus --exclude tmux,archive --no-fetch linux --pull ~/repo ~/scripts --abs
# ───────┴────────────────────────────────────────────────────────────────────────────────────
git-rstatus-update.sh
```

## Feature List - CLI

- Terminal Rendering
    - [x] Dynamic rendering for table
    - [x] 16-color support
    - [ ] Detect non-interactive then drop colors and dynamic rendering
    - [x] Render a braile-style spinner on global progress line
- Git commands
    - [x] `git fetch`
    - [x] `git pull` only if behind and not dirty
    - [ ] `git remote` icons for github, etc
- [x] Export to json
- [ ] Async scan phase
- [ ] Create a detailed markdown report will all git output
    - [ ] Open report in browser?
- [x] `--exclude path,path,path`
- [x] `--help` text and man-style doc file
- [ ] Support shell command completion
- [ ] Cache  git dirs from previos run (with bacground rescan)

## Roadmap

1. Get CLI stable and clean
2. CLI integration with `fzf`
3. TUI version for added Quality-Of-Life features

### Roadmap: Feature List - TUI

- [ ] Scrollable (Up, Down, PgUp, PgDown, Home, End)
- [ ] Seachable `fzf` algo lib?
- [ ] Flyout details
- [ ] Pull on demand
- [ ] Jump - shell integration jump to folder
- [ ] Jump to remote GitHub / GitLab
- [ ] Open - Open folder in app (`vscode`, `vim`, `rider`, etc)

## Project Tasks
- Manual Publish
    - [x] Publish linux release on GitHub
    - [x] Publish windows release on GitHub
    - [ ] Publish to arch `AUR`
    - [ ] Publish to windows `scoop` package manager
- Automatic Publish process

# Command line options

```bash
git-rstatus: Fast recursive git status (with fetch and pull)
   version: 0.4.1
   project: https://github.com/guylangston/git-rstatus

git-rstatus -switch --param path1 path2 path3
    --no-fetch-all              # dont `git fetch` before `git status`
    --no-fetch path,path        # same as above, but only on matching path
    --exclude path,path         # dont process repos containing these strings
    -p, --pull                  # pull (if status is not dirty)
    -a, --abs                   # use absolute paths
    -v, --version               # version information
    -s, --scan-only             # just scan for all git folders and display
    --depth number              # don't recurse deeper than `number`
    --log                       # create log file (in $PWD)
    --json                      # export to json (no other ouptut)

(*) -switch (single char) can be combined, for example -ap will pull and abs paths
```
