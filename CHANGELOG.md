# Changelog

## [1.4.0]

### Added

- `RadioMenu.RememberSelection` (default `false`): the highlighted (and locked) item is remembered per player when the
  radio is put away, and the menu opens on it again when the radio is equipped.
- `RadioMenu.AllowDropping` (default `true`): set it to `false` so players can't drop or throw the radio of the menu.
  Such a radio is also removed (not left on the ground) when the player dies or disconnects.
- `RadioMenuManager.GetSelectedIndex(player, menu)` / `player.GetSelectedRadioMenuIndex(menu)`: the selection of a
  specific menu, or the remembered one while the radio isn't in hand (`-1` if there is none).
- `RadioMenuManager.SetSelectedIndex(player, index)` / `player.SetSelectedRadioMenuIndex(index)`: highlights an item in
  the open menu and redraws it.
- `RadioMenuManager.ForgetSelections(menu)` and `RadioMenuManager.ForgetSelections(player)`. Remembered selections are
  also forgotten when the player leaves and in `ClearAll()`.

### Fixed

- The hint shown when the menu opens always drew the first item, even if an `OnOpened` callback changed the selection.
- The menu was closed as soon as a player *tried* to drop the radio. If another plugin canceled the drop, the radio
  stayed in hand with a dead menu. The menu is now closed only after the radio was really dropped.

## [1.3.3]

- Updated BearmanAPI.

## [1.3.2]

- Menus given with `GiveRadioMenu` are found even when the radio is equipped automatically before the menu is
  assigned to it (pending menus), and the menu events are fired in the right order.

## [1.3.1]

- Updated BearmanAPI.
- Updated to LabAPI 1.1.7.

## [1.3.0]

- Added BearmanAPI version check.
- Added `RadioMenuManager.IsMenuOpen`.
- Redesigned the `Pager` display mode.

## [1.2.0]

- Added config options to translate the texts of the menu hint.
- Fixed the blinking menu hint.
- Menus are closed properly when the player changes or loses the radio.

## [1.1.0]

- Added `MenuDisplayMode` (`List` / `Pager`).
- Added `RadioMenu.LockOnSelect`.
- The menu hint is refreshed while the radio is in hand.
- Menu state is cleaned up when the player's role changes.

## [1.0.1]

- Extension classes use `this` extension methods.
- Added null checks.
- NuGet package updates.

## [1.0.0]

- Initial release.
