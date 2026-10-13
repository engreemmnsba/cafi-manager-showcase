// SAMPLE 4 — Offline-first data layer (Flutter + Riverpod + sembast)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: screens render cached data instantly and refresh in the background (stale-while-
// revalidate), rebuilding only when the data actually changed. Cache keys are namespaced per café and
// wiped on logout, so a shared device never shows one café's data to another account.
// 
// This file: memory + on-disk cache with per-café keys and I/O timeouts.
// ---------------------------------------------------------------------------

import 'dart:async';

import 'package:sembast/sembast.dart';

import 'local_db.dart';

class OfflineStore {
  static final _store = StoreRef<String, Object?>('offline_cache');
  static const _ioTimeout = Duration(seconds: 2);

  /// كاش ذاكرة فوري — قبل/بدل IndexedDB حتى الاستقبال ما يتأخر.
  static final Map<String, Map<String, dynamic>> _memMaps = {};
  static final Map<String, List<Map<String, dynamic>>> _memLists = {};

  /// مفتاح كاش معزول لكل كافيه — يمنع اختلاط منتجات/بيانات محل بآخر على نفس الجهاز.
  static String cafeKey(String? cafeId, String name) {
    final id = (cafeId ?? '').trim();
    if (id.isEmpty) return name;
    return 'cafe_${id}_$name';
  }

  static void clearMemory() {
    _memMaps.clear();
    _memLists.clear();
  }

  /// عند تسجيل الخروج / تبديل حساب: امسح الكاش حتى ما تطلع بيانات محل تاني.
  static Future<void> clearAll() async {
    clearMemory();
    Future<void> wipe() async {
      final db = await LocalDb.instance.database.timeout(const Duration(seconds: 5));
      await _store.delete(db).timeout(const Duration(seconds: 5));
    }

    try {
      await wipe();
    } catch (_) {
      clearMemory();
      try {
        await wipe();
      } catch (_) {
        // إن فشل المسح من القرص تبقى الذاكرة فارغة؛ الجيل التالي من الكاش يمنع العرض القديم.
      }
    }
  }

  static void putMapMemory(String key, Map<String, dynamic> value) {
    _memMaps[key] = Map<String, dynamic>.from(value);
  }

  static void putListMemory(String key, List<Map<String, dynamic>> value) {
    _memLists[key] = [for (final e in value) Map<String, dynamic>.from(e)];
  }

  static Future<void> saveMap(String key, Map<String, dynamic> value) async {
    putMapMemory(key, value);
    try {
      final db = await LocalDb.instance.database.timeout(_ioTimeout);
      await _store.record(key).put(db, value).timeout(_ioTimeout);
    } catch (_) {
      // لا نوقف الواجهة بسبب كاش محلي.
    }
  }

  static Future<Map<String, dynamic>?> readMap(String key) async {
    final mem = _memMaps[key];
    if (mem != null) return Map<String, dynamic>.from(mem);
    try {
      final db = await LocalDb.instance.database.timeout(_ioTimeout);
      final raw = await _store.record(key).get(db).timeout(_ioTimeout);
      if (raw is Map<String, dynamic>) {
        putMapMemory(key, raw);
        return raw;
      }
      if (raw is Map) {
        final mapped = Map<String, dynamic>.from(raw);
        putMapMemory(key, mapped);
        return mapped;
      }
      return null;
    } catch (_) {
      return null;
    }
  }

  static Future<void> saveList(String key, List<Map<String, dynamic>> value) async {
    putListMemory(key, value);
    try {
      final db = await LocalDb.instance.database.timeout(_ioTimeout);
      await _store.record(key).put(db, value).timeout(_ioTimeout);
    } catch (_) {}
  }

  static Future<List<Map<String, dynamic>>?> readList(String key) async {
    final mem = _memLists[key];
    if (mem != null) return [for (final e in mem) Map<String, dynamic>.from(e)];
    try {
      final db = await LocalDb.instance.database.timeout(_ioTimeout);
      final raw = await _store.record(key).get(db).timeout(_ioTimeout);
      if (raw is! List) return null;
      final mapped = raw.map<Map<String, dynamic>>((e) {
        if (e is Map<String, dynamic>) return e;
        return Map<String, dynamic>.from(e as Map);
      }).toList();
      putListMemory(key, mapped);
      return mapped;
    } catch (_) {
      return null;
    }
  }
}
