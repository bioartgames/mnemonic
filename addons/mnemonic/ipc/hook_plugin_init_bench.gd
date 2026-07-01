class_name HookPluginInitBench
extends RefCounted

const DataRootPathsGd = preload("res://addons/mnemonic/ipc/data_root_paths.gd")
const HookGitPollGd = preload("res://addons/mnemonic/git/hook_git_poll.gd")
const HookCoreRunningCacheGd = preload("res://addons/mnemonic/ipc/hook_core_running_cache.gd")
const HookCoreProcessProbeGd = preload("res://addons/mnemonic/ipc/hook_core_process_probe.gd")
const HookLifecyclePollerGd = preload("res://addons/mnemonic/ipc/hook_lifecycle_poller.gd")
const HookStatusReaderGd = preload("res://addons/mnemonic/ipc/hook_status_reader.gd")
const HookClipsIndexGd = preload("res://addons/mnemonic/clips/hook_clips_index.gd")
const HookClipThumbnailGd = preload("res://addons/mnemonic/clips/hook_clip_thumbnail.gd")
const Mc = preload("res://addons/mnemonic/ipc/mnemonic_constants.gd")


static func measure_ms(callable: Callable) -> int:
	var start_us := Time.get_ticks_usec()
	callable.call()
	return int((Time.get_ticks_usec() - start_us) / 1000)


static func seed_clip_fixtures(root: String, clip_count: int, with_thumbnails: bool) -> void:
	var paths := DataRootPathsGd.new(root)
	DirAccess.make_dir_recursive_absolute(paths.get_clips_dir())
	for i in clip_count:
		var segment_name := "segment_%05d" % i
		var folder := paths.get_clips_dir().path_join(segment_name)
		DirAccess.make_dir_recursive_absolute(folder)
		var json_path := folder.path_join("clip.json")
		var body := JSON.stringify({
			"id": segment_name,
			"created_at": 1_700_000_000 + i,
			"score": i % 20,
			"duration_seconds": 120,
			"commit_subject": "Bench clip %d" % i,
			"git_branch": "main",
			"scenes_active": ["res://bench/scene_%d.tscn" % (i % 3)],
			"tags": ["playtest"],
		})
		var file := FileAccess.open(json_path, FileAccess.WRITE)
		if file == null:
			push_error("bench: could not write %s" % json_path)
			continue
		file.store_string(body)
		file.close()
		if with_thumbnails:
			_write_tiny_thumb(folder.path_join(Mc.CLIP_THUMB_FILE_NAME))


static func _write_tiny_thumb(thumb_abs: String) -> void:
	var img := Image.create(64, 36, false, Image.FORMAT_RGBA8)
	img.fill(Color(0.2, 0.25, 0.35, 1.0))
	img.save_png(thumb_abs)


static func run_phases(clip_count: int = 50) -> Dictionary:
	var results: Dictionary = {}
	var temp_root := OS.get_cache_dir().path_join(
		"mnemonic_init_bench_%d" % Time.get_ticks_msec()
	)
	DirAccess.make_dir_recursive_absolute(temp_root)
	var paths := DataRootPathsGd.new(temp_root)

	seed_clip_fixtures(temp_root, clip_count, true)

	results["git_baseline_deferred_ms"] = measure_ms(func() -> void:
		var git := HookGitPollGd.new(paths)
		git.tick()
	)

	if OS.get_name() == "Windows":
		results["tasklist_probe_ms"] = measure_ms(func() -> void:
			HookCoreProcessProbeGd.is_running()
		)
	else:
		results["tasklist_probe_ms"] = 0

	results["enter_tree_light_ms"] = measure_ms(func() -> void:
		HookCoreRunningCacheGd.read(true)
		var reader := HookStatusReaderGd.new(paths)
		reader.read()
	)

	results["lifecycle_poll_ms"] = measure_ms(func() -> void:
		var poller := HookLifecyclePollerGd.new(paths)
		poller.poll(false)
	)

	var thumb_paths: PackedStringArray = PackedStringArray()
	results["clips_index_list_ms"] = measure_ms(func() -> void:
		var clips := HookClipsIndexGd.list_clips(paths, 10)
		if clips.size() != clip_count:
			push_error("bench: expected %d clips, got %d" % [clip_count, clips.size()])
		for row in clips:
			var thumb_abs := str(row.get("thumb_abs", ""))
			if not thumb_abs.is_empty():
				thumb_paths.append(thumb_abs)
	)

	results["thumbnail_load_ms"] = measure_ms(func() -> void:
		for thumb_abs in thumb_paths:
			HookClipThumbnailGd.try_load_texture(thumb_abs)
	)

	results["clip_count"] = clip_count
	results["thumb_count"] = thumb_paths.size()

	var dominant := ""
	var dominant_ms := -1
	for key in results.keys():
		if not str(key).ends_with("_ms"):
			continue
		var ms := int(results[key])
		if ms > dominant_ms:
			dominant_ms = ms
			dominant = str(key)

	results["dominant_phase"] = dominant
	results["dominant_ms"] = dominant_ms

	_remove_dir_recursive(temp_root)
	return results


static func _remove_dir_recursive(path: String) -> void:
	if not DirAccess.dir_exists_absolute(path):
		return
	var dir := DirAccess.open(path)
	if dir == null:
		return
	dir.erase_contents_recursive()
	DirAccess.remove_absolute(path)
