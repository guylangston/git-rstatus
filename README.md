# git-status

> Fast recursive scan for all git repos. Async `git fetch && git status`

## Feature List - CLI

- Terminal Rendering
    - [x] Dynamic rendering for table
    - [x] 16-color support
    - [ ] 256-color support
    - [ ] fast rendering (ncurses, etc)
    - [ ] Detect non-interactive then drop colors
- Git commands
    - [x] `git fetch`
    - [x] `git pull` only if behind and not dirty
    - [ ] `git remote` icons for github, etc
- Export to json
- Async scan phase

## Wish List (not current plan for implementation)

- User interaction - TUI
    - Move Up/Down, Scroll Up/Down
    - Sidebar-style flyout with more information
    - Select a git repo and open in Explorer or terminal
- Logger: write all `git` output to a file


