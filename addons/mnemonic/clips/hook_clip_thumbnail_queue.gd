class_name HookClipThumbnailQueue
extends RefCounted

const HookClipThumbnailGd = preload("res://addons/mnemonic_hook/clips/hook_clip_thumbnail.gd")

var _generation := 0
var _pending: Array[Dictionary] = []


func reset(generation: int) -> void:
	_generation = generation
	_pending.clear()


func enqueue(generation: int, thumb_abs: String, rect: TextureRect) -> void:
	if thumb_abs.is_empty() or rect == null:
		return
	_pending.append({
		"generation": generation,
		"thumb_abs": thumb_abs,
		"rect_id": rect.get_instance_id(),
	})


func has_pending() -> bool:
	return not _pending.is_empty()


func process_batch(active_generation: int, batch_size: int) -> bool:
	var processed := 0
	while processed < batch_size and not _pending.is_empty():
		var entry: Dictionary = _pending[0]
		_pending.remove_at(0)
		processed += 1
		if int(entry.get("generation", -1)) != active_generation:
			continue
		var rect_id := int(entry.get("rect_id", 0))
		if rect_id == 0:
			continue
		var rect: Object = instance_from_id(rect_id)
		if rect == null or not rect is TextureRect:
			continue
		var thumb_abs := str(entry.get("thumb_abs", ""))
		var tex := HookClipThumbnailGd.try_load_texture(thumb_abs)
		var thumb_rect := rect as TextureRect
		if tex != null:
			thumb_rect.texture = tex
			thumb_rect.modulate = Color.WHITE
	return not _pending.is_empty()
