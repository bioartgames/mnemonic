class_name HookSignificanceTier
extends RefCounted

const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")
const HookDockThemeGd = preload("res://addons/mnemonic/ui/hook_dock_theme.gd")


static func resolve_notable_score_min(
	notable_score_min: int,
	preserve_threshold: int,
) -> int:
	if notable_score_min >= 0:
		return notable_score_min
	if preserve_threshold >= 0:
		return preserve_threshold
	return -1


static func manual_preserve_queued(
	live_segment_index: int,
	pending_manual_preserve_segment: int = -1,
	live_save_pending: bool = false,
	live_save_segment_index: int = -1,
) -> bool:
	if live_segment_index < 0:
		return false
	if pending_manual_preserve_segment == live_segment_index:
		return true
	return live_save_pending and live_save_segment_index == live_segment_index


static func resolve_live_rec_tier(
	score: int,
	preserve_threshold: int,
	highlight_score_min: int,
	notable_score_min: int,
	live_segment_index: int,
	pending_manual_preserve_segment: int = -1,
	live_save_pending: bool = false,
	live_save_segment_index: int = -1,
) -> String:
	if manual_preserve_queued(
		live_segment_index,
		pending_manual_preserve_segment,
		live_save_pending,
		live_save_segment_index,
	):
		return Mc.LIVE_REC_TIER_QUEUED_SAVE
	return classify_tier(
		score, preserve_threshold, highlight_score_min, notable_score_min
	)


static func classify_tier(
	score: int,
	preserve_threshold: int,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> String:
	var highlight_min := highlight_score_min
	if highlight_min < 0:
		highlight_min = Mc.SETTINGS_DEFAULT_HIGHLIGHT_SCORE_MIN
	var notable_min := resolve_notable_score_min(notable_score_min, preserve_threshold)
	if preserve_threshold >= 0 and score < preserve_threshold:
		return "manual"
	if score >= highlight_min:
		return "highlight"
	if notable_min >= 0 and score >= notable_min:
		return "notable"
	return ""


static func tier_label(tier_id: String) -> String:
	match tier_id:
		"manual":
			return Mc.SIGNIFICANCE_TIER_LABEL_MANUAL
		"notable":
			return Mc.SIGNIFICANCE_TIER_LABEL_NOTABLE
		"highlight":
			return Mc.SIGNIFICANCE_TIER_LABEL_HIGHLIGHT
		_:
			return ""


static func format_score_tooltip_line(
	score: int,
	preserve_threshold: int,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
	is_live: bool = false,
) -> String:
	var tier_id := classify_tier(
		score, preserve_threshold, highlight_score_min, notable_score_min
	)
	if is_live and tier_id == "manual":
		return "Score: %d (%s)" % [score, Mc.TOOLTIP_LIVE_SCORE_BELOW_THRESHOLD]
	if tier_id.is_empty():
		return "Score: %d" % score
	return "Score: %d (%s)" % [score, tier_label(tier_id)]


static func queued_save_rec_dot_color(theme: HookDockTheme) -> Color:
	if theme != null:
		var accent: Color = theme.color_editor("accent_color")
		accent.a = 0.92
		return accent
	return Color(0.45, 0.65, 0.95, 0.92)


static func tier_border_color(tier_id: String, theme: HookDockTheme) -> Color:
	if tier_id == Mc.LIVE_REC_TIER_QUEUED_SAVE:
		return queued_save_rec_dot_color(theme)
	if tier_id.strip_edges().is_empty():
		return neutral_rec_dot_color(theme)
	if theme != null:
		match tier_id:
			"highlight":
				return theme.color_editor("warning_color")
			"notable":
				return theme.color_editor("accent_color")
			"manual":
				return theme.color_editor("readonly_color")
			_:
				return neutral_rec_dot_color(theme)
	return Color(0.7, 0.7, 0.7, 0.8)


static func tier_bg_color(tier_id: String, theme: HookDockTheme) -> Color:
	if theme != null:
		match tier_id:
			"highlight":
				var highlight_bg: Color = theme.color_editor("warning_color")
				highlight_bg.a = 0.18
				return highlight_bg
			"notable":
				var notable_bg: Color = theme.color_editor("accent_color")
				notable_bg.a = 0.18
				return notable_bg
			"manual":
				var manual_bg: Color = theme.color_editor("readonly_color")
				manual_bg.a = 0.25
				return manual_bg
			_:
				return Color(0.3, 0.3, 0.3, 0.35)
	return Color(0.3, 0.3, 0.3, 0.35)


static func neutral_rec_dot_color(theme: HookDockTheme) -> Color:
	if theme != null:
		var neutral: Color = theme.color_editor("readonly_color")
		neutral.a = 0.85
		return neutral
	return Color(0.55, 0.55, 0.55, 0.85)


static func tier_badge_tooltip(
	tier_id: String,
	score: int,
	preserve_threshold: int,
	highlight_score_min: int = -1,
	notable_score_min: int = -1,
) -> String:
	var label := tier_label(tier_id)
	if label.is_empty():
		return format_score_tooltip_line(
			score, preserve_threshold, highlight_score_min, notable_score_min
		)
	return "%s — %s" % [
		label,
		format_score_tooltip_line(
			score, preserve_threshold, highlight_score_min, notable_score_min
		),
	]


static func apply_tier_badge_style(
	panel: PanelContainer,
	tier_id: String,
	theme: HookDockTheme,
) -> void:
	var score_style := StyleBoxFlat.new()
	score_style.border_width_left = 1
	score_style.border_width_top = 1
	score_style.border_width_right = 1
	score_style.border_width_bottom = 1
	score_style.corner_radius_top_left = 3
	score_style.corner_radius_top_right = 3
	score_style.corner_radius_bottom_left = 3
	score_style.corner_radius_bottom_right = 3
	score_style.bg_color = tier_bg_color(tier_id, theme)
	score_style.border_color = tier_border_color(tier_id, theme)
	panel.add_theme_stylebox_override("panel", score_style)
