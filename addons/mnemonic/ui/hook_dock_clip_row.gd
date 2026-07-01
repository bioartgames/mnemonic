class_name HookDockClipRow
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookDockVerticalLayoutGd = preload("res://addons/mnemonic/ui/hook_dock_vertical_layout.gd")
const HookClipThumbnailGd = preload("res://addons/mnemonic/clips/hook_clip_thumbnail.gd")
const HookSignificanceTierGd = preload("res://addons/mnemonic/clips/hook_significance_tier.gd")
const HookLiveRecIndicatorGd = preload("res://addons/mnemonic/ui/hook_live_rec_indicator.gd")
const HookDockPopupUtilsGd = preload("res://addons/mnemonic/ui/hook_dock_popup_utils.gd")

const MENU_REVEAL := 0
const MENU_PLAY := 1
const MENU_DELETE := 2
const MENU_SAVE_SEGMENT := 3
const MENU_COPY_METADATA := 4


class ClipRowContext:
	var theme: HookDockTheme = null
	var thumb_size: Vector2 = Vector2.ZERO
	var scaled_px_fn: Callable = Callable()
	var preserve_threshold: int = -1
	var highlight_score_min: int = -1
	var notable_score_min: int = -1
	var thumb_generation: int = 0
	var thumb_enqueue_fn: Callable = Callable()
	var style_label_fn: Callable = Callable()
	var apply_tooltip_fn: Callable = Callable()
	var pending_manual_preserve_segment: int = -1
	var live_save_pending: bool = false
	var live_save_segment_index: int = -1


class ClipRowMenuContext:
	var theme: HookDockTheme = null
	var info: Dictionary = {}
	var thumb_size: Vector2 = Vector2.ZERO
	var is_live_row: bool = false
	var can_save_live_segment: Callable = Callable()
	var on_menu_pressed: Callable = Callable()
	var on_popup_hidden: Callable = Callable()


static func thumb_size(compact: bool = false) -> Vector2:
	if compact:
		return Vector2(Mc.THUMB_COMPACT_WIDTH_PX, Mc.THUMB_COMPACT_HEIGHT_PX)
	return Vector2(Mc.CLIP_THUMB_WIDTH_PX, Mc.CLIP_THUMB_HEIGHT_PX)


static func build_archived_hbox(info: Dictionary, ctx: ClipRowContext) -> HBoxContainer:
	var hb := HBoxContainer.new()
	hb.custom_minimum_size.x = 0
	hb.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(hb)
	if ctx.theme != null:
		hb.add_theme_constant_override("separation", ctx.theme.row_separation_px())

	var tooltip := str(info.get("tooltip", ""))
	var thumb_size_v := ctx.thumb_size

	var thumb_rect := TextureRect.new()
	var thumb_abs := str(info.get("thumb_abs", ""))
	HookClipThumbnailGd.apply_placeholder(thumb_rect)
	thumb_rect.custom_minimum_size = thumb_size_v
	if ctx.thumb_enqueue_fn.is_valid():
		ctx.thumb_enqueue_fn.call(ctx.thumb_generation, thumb_abs, thumb_rect)
	thumb_rect.size_flags_horizontal = Control.SIZE_SHRINK_BEGIN
	_apply_tooltip(ctx, thumb_rect, tooltip)
	hb.add_child(thumb_rect)

	var tier_id := str(info.get("significance_tier_id", "")).strip_edges()
	if not tier_id.is_empty():
		var badge_wrap := CenterContainer.new()
		badge_wrap.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		var tier_badge := PanelContainer.new()
		var badge_px := _scaled_px(ctx, 10)
		tier_badge.custom_minimum_size = Vector2(badge_px, badge_px)
		HookSignificanceTierGd.apply_tier_badge_style(tier_badge, tier_id, ctx.theme)
		var score := int(info.get("score", 0))
		tier_badge.tooltip_text = HookSignificanceTierGd.tier_badge_tooltip(
			tier_id,
			score,
			ctx.preserve_threshold,
			ctx.highlight_score_min,
			ctx.notable_score_min,
		)
		_apply_tooltip(ctx, tier_badge, tooltip)
		badge_wrap.add_child(tier_badge)
		hb.add_child(badge_wrap)

	var title_row := HBoxContainer.new()
	title_row.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	title_row.size_flags_stretch_ratio = 1.0
	title_row.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	title_row.custom_minimum_size.y = thumb_size_v.y
	HookDockVerticalLayoutGd.apply_hbox_center(title_row)
	_apply_tooltip(ctx, title_row, tooltip)

	var lab := Label.new()
	lab.text = str(info.get("display_title", info.get("id", "?")))
	lab.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	lab.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	_style_label(ctx, lab)
	_apply_tooltip(ctx, lab, tooltip)
	if ctx.theme != null:
		ctx.theme.apply_shrink_width_label(lab, false)
		HookDockVerticalLayoutGd.apply_label_center(lab)
	title_row.add_child(lab)
	hb.add_child(title_row)

	return hb


static func build_live_hbox(info: Dictionary, ctx: ClipRowContext) -> Dictionary:
	var thumb_size_v := ctx.thumb_size
	var tooltip := str(info.get("tooltip", ""))

	var hb := HBoxContainer.new()
	hb.custom_minimum_size.x = 0
	hb.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	HookDockVerticalLayoutGd.apply_hbox_center(hb)
	if ctx.theme != null:
		hb.add_theme_constant_override("separation", ctx.theme.row_separation_px())

	var live_row_band := HBoxContainer.new()
	live_row_band.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	live_row_band.size_flags_stretch_ratio = 1.0
	HookDockVerticalLayoutGd.apply_thumb_band_center(live_row_band, thumb_size_v.y)
	HookDockVerticalLayoutGd.apply_hbox_center(live_row_band)
	if ctx.theme != null:
		live_row_band.add_theme_constant_override("separation", ctx.theme.row_separation_px())
	_apply_tooltip(ctx, live_row_band, tooltip)

	var live_rec_indicator := HookLiveRecIndicatorGd.build(thumb_size_v, ctx.theme)
	var indicator_root: Control = live_rec_indicator["root"]
	indicator_root.set_meta(&"mnemonic_live_indicator", true)
	indicator_root.set_meta(&"mnemonic_indicator_state", live_rec_indicator)
	_apply_tooltip(ctx, indicator_root, tooltip)
	live_row_band.add_child(indicator_root)

	var tier_id := str(info.get("live_rec_tier_id", "")).strip_edges()
	if tier_id.is_empty():
		tier_id = HookSignificanceTierGd.resolve_live_rec_tier(
			int(info.get("score", 0)),
			ctx.preserve_threshold,
			ctx.highlight_score_min,
			ctx.notable_score_min,
			int(info.get("live_segment_index", -1)),
			ctx.pending_manual_preserve_segment,
			ctx.live_save_pending,
			ctx.live_save_segment_index,
		)
	HookLiveRecIndicatorGd.apply_tier(live_rec_indicator, tier_id, ctx.theme)

	var lab := Label.new()
	lab.text = str(info.get("display_title", info.get("id", "?")))
	_style_label(ctx, lab)
	_apply_tooltip(ctx, lab, tooltip)
	if ctx.theme != null:
		ctx.theme.apply_body_font(lab)

	var timer_slot := HookDockVerticalLayoutGd.wrap_label_for_thumb_band(lab, thumb_size_v)
	timer_slot.set_meta(&"mnemonic_live_timer_slot", true)
	_apply_tooltip(ctx, timer_slot, tooltip)
	live_row_band.add_child(timer_slot)

	hb.add_child(live_row_band)
	hb.custom_minimum_size.y = thumb_size_v.y

	return {
		"hb": hb,
		"live_row_band": live_row_band,
		"lab": lab,
		"live_rec_indicator": live_rec_indicator,
	}


static func wrap_live_panel(hb: HBoxContainer, theme: HookDockTheme) -> PanelContainer:
	var live_panel := PanelContainer.new()
	live_panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	live_panel.size_flags_vertical = Control.SIZE_SHRINK_BEGIN
	if theme != null:
		live_panel.add_theme_stylebox_override("panel", theme.make_live_row_panel())
	live_panel.set_meta(&"mnemonic_live_row", true)
	HookDockVerticalLayoutGd.apply_thumb_band_center(hb, hb.custom_minimum_size.y)
	live_panel.add_child(hb)
	return live_panel


static func build_placeholder_menu(thumb_size_v: Vector2, theme: HookDockTheme) -> MenuButton:
	var menu := MenuButton.new()
	menu.text = ""
	menu.size_flags_horizontal = Control.SIZE_SHRINK_END
	menu.vertical_icon_alignment = VERTICAL_ALIGNMENT_CENTER
	if theme != null:
		theme.apply_flat_icon_button(menu)
	menu.custom_minimum_size.y = thumb_size_v.y
	return menu


static func build_live_menu_slot(
	thumb_size_v: Vector2, theme: HookDockTheme, menu: MenuButton
) -> CenterContainer:
	var menu_slot := CenterContainer.new()
	menu_slot.custom_minimum_size.x = thumb_size_v.x
	menu_slot.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	menu_slot.set_meta(&"mnemonic_live_menu_slot", true)
	menu_slot.add_child(
		HookDockVerticalLayoutGd.wrap_menu_for_thumb_band(menu, thumb_size_v)
	)
	return menu_slot


static func build_live_row_root(
	info: Dictionary, ctx: ClipRowContext, wrap_panel: bool
) -> Control:
	var built := build_live_hbox(info, ctx)
	var menu := build_placeholder_menu(ctx.thumb_size, ctx.theme)
	built.live_row_band.add_child(build_live_menu_slot(ctx.thumb_size, ctx.theme, menu))
	if wrap_panel and ctx.theme != null:
		return wrap_live_panel(built.hb, ctx.theme)
	return built.hb


static func build_archived_probe_row(info: Dictionary, ctx: ClipRowContext) -> HBoxContainer:
	var hb := build_archived_hbox(info, ctx)
	hb.custom_minimum_size.x = 400
	hb.custom_minimum_size.y = ctx.thumb_size.y
	var menu := build_placeholder_menu(ctx.thumb_size, ctx.theme)
	menu.size_flags_vertical = Control.SIZE_SHRINK_CENTER
	hb.add_child(menu)
	return hb


static func wire_production_menu(
	hb: HBoxContainer,
	live_row_band: HBoxContainer,
	ctx: ClipRowMenuContext,
) -> MenuButton:
	var folder_abs := str(ctx.info.get("folder_abs", ""))
	var reveal_abs := str(ctx.info.get("reveal_abs", folder_abs))
	var video_abs := str(ctx.info.get("video_abs", ""))
	var display_title := str(ctx.info.get("display_title", ctx.info.get("id", "?")))
	var tooltip_text := str(ctx.info.get("tooltip", "")).strip_edges()

	var menu := MenuButton.new()
	menu.text = ""
	menu.tooltip_text = (
		Mc.TOOLTIP_SEGMENT_ACTIONS if ctx.is_live_row else Mc.TOOLTIP_CLIP_ACTIONS
	)
	menu.size_flags_horizontal = Control.SIZE_SHRINK_END
	menu.vertical_icon_alignment = VERTICAL_ALIGNMENT_CENTER
	if ctx.theme != null:
		ctx.theme.apply_flat_icon_button(menu)
		var menu_icon: Texture2D = ctx.theme.icon(&"GuiTabMenuHl")
		if menu_icon == null:
			menu_icon = ctx.theme.icon(&"MoreOptions")
		if menu_icon != null:
			menu.icon = menu_icon
	var popup := menu.get_popup()
	var show_reveal := true
	if ctx.is_live_row:
		show_reveal = bool(ctx.info.get("show_reveal", false))
	if show_reveal:
		popup.add_item(Mc.UI_MENU_REVEAL, MENU_REVEAL)
	if ctx.is_live_row:
		popup.add_item(Mc.UI_SAVE_SEGMENT, MENU_SAVE_SEGMENT)
	else:
		popup.add_item(Mc.UI_PLAY_VIDEO, MENU_PLAY)
	popup.add_item(Mc.UI_COPY_CLIP_METADATA, MENU_COPY_METADATA)
	if not ctx.is_live_row:
		popup.add_item(Mc.UI_DELETE_CLIP, MENU_DELETE)
	HookDockPopupUtilsGd.set_item_disabled(
		popup, MENU_COPY_METADATA, tooltip_text.is_empty()
	)
	if not ctx.is_live_row:
		HookDockPopupUtilsGd.set_item_disabled(
			popup, MENU_REVEAL, not DirAccess.dir_exists_absolute(reveal_abs)
		)
	if ctx.is_live_row:
		HookDockPopupUtilsGd.set_item_disabled(
			popup, MENU_SAVE_SEGMENT, not bool(ctx.can_save_live_segment.call())
		)
	else:
		HookDockPopupUtilsGd.set_item_disabled(
			popup, MENU_PLAY, not FileAccess.file_exists(video_abs)
		)
		HookDockPopupUtilsGd.set_item_disabled(
			popup, MENU_DELETE, not DirAccess.dir_exists_absolute(folder_abs)
		)
	popup.id_pressed.connect(
		func(id: int) -> void:
			if ctx.on_menu_pressed.is_valid():
				ctx.on_menu_pressed.call(
					id, reveal_abs, video_abs, display_title, ctx.is_live_row, tooltip_text
				)
	)
	popup.about_to_popup.connect(
		func() -> void:
			HookDockPopupUtilsGd.set_item_disabled(
				popup, MENU_COPY_METADATA, tooltip_text.is_empty()
			)
			if not ctx.is_live_row:
				HookDockPopupUtilsGd.set_item_disabled(
					popup,
					MENU_REVEAL,
					not DirAccess.dir_exists_absolute(reveal_abs),
				)
				return
			HookDockPopupUtilsGd.set_item_disabled(
				popup,
				MENU_SAVE_SEGMENT,
				not bool(ctx.can_save_live_segment.call()),
			)
	)
	if ctx.on_popup_hidden.is_valid() and not popup.popup_hide.is_connected(ctx.on_popup_hidden):
		popup.popup_hide.connect(ctx.on_popup_hidden)
	if ctx.is_live_row and live_row_band != null:
		live_row_band.add_child(build_live_menu_slot(ctx.thumb_size, ctx.theme, menu))
	else:
		menu.size_flags_vertical = Control.SIZE_SHRINK_CENTER
		menu.custom_minimum_size.y = ctx.thumb_size.y
		hb.add_child(menu)
	return menu


static func stress_tall_row_hbox(hb: HBoxContainer) -> void:
	hb.size_flags_vertical = Control.SIZE_EXPAND_FILL
	hb.alignment = BoxContainer.ALIGNMENT_BEGIN


static func center_y(control: Control) -> float:
	var rect := control.get_global_rect()
	return rect.position.y + rect.size.y * 0.5


static func measure_row_band(archived_hb: HBoxContainer, live_root: Control) -> Dictionary:
	var archived_thumb := archived_hb.get_child(0) as Control
	var archived_title := (archived_hb.get_child(2) as HBoxContainer).get_child(0) as Control
	var archived_menu := _menu_from_slot(archived_hb.get_child(archived_hb.get_child_count() - 1))

	var live_hb := live_root as HBoxContainer
	if live_hb == null and live_root is PanelContainer:
		live_hb = live_root.get_child(0) as HBoxContainer
	if live_hb == null:
		return {"ok": false, "error": "live row missing hbox"}

	var live_row_band := live_hb.get_child(0) as HBoxContainer
	if live_row_band == null:
		return {"ok": false, "error": "live row missing band hbox"}
	var live_thumb := live_row_band.get_child(0) as Control
	var live_timer := (live_row_band.get_child(1) as CenterContainer).get_child(0) as Control
	var dot_wrap := live_thumb.get_child(1) as CenterContainer
	var dot_panel := dot_wrap.get_child(0) as PanelContainer
	var live_dot := dot_panel.get_child(0) as Control
	var live_menu := _menu_from_slot(live_row_band.get_child(live_row_band.get_child_count() - 1))

	var ys := {
		"archived_thumb": center_y(archived_thumb),
		"archived_title": center_y(archived_title),
		"archived_menu": center_y(archived_menu),
		"live_thumb": center_y(live_thumb),
		"live_dot": center_y(live_dot),
		"live_timer": center_y(live_timer),
		"live_menu": center_y(live_menu),
		"live_hb": center_y(live_hb),
		"live_root": center_y(live_root),
	}
	ys["archived_hb"] = center_y(archived_hb)
	ys["live_hb_h"] = live_hb.size.y
	ys["live_root_h"] = live_root.size.y
	ys["archived_hb_h"] = archived_hb.size.y
	ys["live_menu_h"] = live_menu.size.y
	return {"ok": true, "ys": ys}


static func archived_row_internal_delta(hb: HBoxContainer) -> float:
	var thumb := hb.get_child(0) as Control
	var title := (hb.get_child(2) as HBoxContainer).get_child(0) as Control
	var menu := _menu_from_slot(hb.get_child(hb.get_child_count() - 1))
	var ys := {
		"archived_thumb": center_y(thumb),
		"archived_title": center_y(title),
		"archived_menu": center_y(menu),
	}
	return max_delta(ys, ["archived_thumb", "archived_title", "archived_menu"])


static func max_delta(ys: Dictionary, keys: Array) -> float:
	if keys.is_empty():
		return 0.0
	var ref: float = ys[keys[0]]
	var max_d := 0.0
	for key in keys:
		max_d = maxf(max_d, absf(float(ys[key]) - ref))
	return max_d


static func _menu_from_slot(slot: Node) -> Control:
	if slot is CenterContainer and slot.get_child_count() > 0:
		var inner := slot.get_child(0)
		if inner is CenterContainer and inner.get_child_count() > 0:
			return inner.get_child(0) as Control
		return inner as Control
	return slot as Control


static func _scaled_px(ctx: ClipRowContext, base_px: int) -> int:
	if ctx.scaled_px_fn.is_valid():
		return int(ctx.scaled_px_fn.call(base_px))
	return base_px


static func _style_label(ctx: ClipRowContext, label: Label) -> void:
	if ctx.style_label_fn.is_valid():
		ctx.style_label_fn.call(label)
	else:
		HookDockVerticalLayoutGd.apply_label_center(label)


static func _apply_tooltip(ctx: ClipRowContext, control: Control, tooltip: String) -> void:
	if control == null or tooltip.is_empty():
		return
	if ctx.apply_tooltip_fn.is_valid():
		ctx.apply_tooltip_fn.call(control, tooltip)
	else:
		control.tooltip_text = tooltip
