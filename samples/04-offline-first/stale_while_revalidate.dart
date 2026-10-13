// SAMPLE 4 — Offline-first data layer (Flutter + Riverpod + sembast)
// Source: Cafi Manager (private repository). Excerpt published for portfolio review.
// 
// What it shows: screens render cached data instantly and refresh in the background (stale-while-
// revalidate), rebuilding only when the data actually changed. Cache keys are namespaced per café and
// wiped on logout, so a shared device never shows one café's data to another account.
// 
// This file: stale-while-revalidate loaders used by Riverpod providers.
// ---------------------------------------------------------------------------

import 'dart:async';
import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'offline_store.dart';

/// يزداد عند تسجيل الخروج / دخول حساب جديد — يفرض إعادة جلب البيانات بدل كاش الحساب السابق.
final cacheGenerationProvider = StateProvider<int>((ref) => 0);

/// مفاتيح كاش يجب جلبها من الشبكة في المرة التالية (سحب للتحديث).
final forceNetworkCacheKeysProvider = StateProvider<Set<String>>((ref) => {});

bool _sameJsonMaps(List<Map<String, dynamic>> a, List<Map<String, dynamic>> b) {
  if (identical(a, b)) return true;
  if (a.length != b.length) return false;
  return jsonEncode(a) == jsonEncode(b);
}

bool _sameJsonMap(Map<String, dynamic> a, Map<String, dynamic> b) {
  if (identical(a, b)) return true;
  return jsonEncode(a) == jsonEncode(b);
}

bool _consumeForceNetwork(Ref ref, String cacheKey) {
  final keys = ref.read(forceNetworkCacheKeysProvider);
  String? matched;
  for (final k in keys) {
    // يدعم المفتاح العادي أو المفتاح المعزول: cafe_{id}_offline_...
    if (cacheKey == k || cacheKey.endsWith('_$k')) {
      matched = k;
      break;
    }
  }
  if (matched == null) return false;
  ref.read(forceNetworkCacheKeysProvider.notifier).update((s) => {...s}
    ..remove(matched)
    ..remove(cacheKey));
  return true;
}

/// يعرض الكاش فوراً ثم يحدّث بالخلفية (بدون ما الشاشة تصير loading).
/// يعيد البناء فقط إذا تغيّر المحتوى — فلا حلقة تحديث مستمرة.
Future<List<Map<String, dynamic>>> loadListStaleWhileRevalidate({
  required Ref ref,
  required String cacheKey,
  required Future<List<Map<String, dynamic>>> Function() fetch,
  List<Map<String, dynamic>> Function(List<Map<String, dynamic>> raw)? mapCached,
  /// إن وُجدت: تُستخدم بدل مقارنة JSON الكاملة (لتجاهل حقول متغيّرة مثل المجاميع الحيّة).
  bool Function(List<Map<String, dynamic>> a, List<Map<String, dynamic>> b)? equals,
}) async {
  // تبديل حساب → لا نعرض كاش الحساب السابق ولو بقي بالقرص لحظة.
  ref.watch(cacheGenerationProvider);
  final rawCached = await OfflineStore.readList(cacheKey);
  final cached = rawCached == null
      ? null
      : (mapCached == null ? rawCached : mapCached(rawCached));
  final forceNetwork = _consumeForceNetwork(ref, cacheKey);
  final same = equals ?? _sameJsonMaps;

  if (cached != null && !forceNetwork) {
    unawaited(() async {
      try {
        final fresh = await fetch();
        await OfflineStore.saveList(cacheKey, fresh);
        if (!same(fresh, cached)) {
          ref.invalidateSelf();
        }
      } catch (_) {
        // نبقى على الكاش.
      }
    }());
    return cached;
  }

  try {
    final fresh = await fetch();
    await OfflineStore.saveList(cacheKey, fresh);
    return fresh;
  } catch (e) {
    if (cached != null) return cached;
    rethrow;
  }
}

Future<Map<String, dynamic>> loadMapStaleWhileRevalidate({
  required Ref ref,
  required String cacheKey,
  required Future<Map<String, dynamic>> Function() fetch,
}) async {
  ref.watch(cacheGenerationProvider);
  final cached = await OfflineStore.readMap(cacheKey);
  final forceNetwork = _consumeForceNetwork(ref, cacheKey);

  if (cached != null && cached.isNotEmpty && !forceNetwork) {
    unawaited(() async {
      try {
        final fresh = await fetch();
        await OfflineStore.saveMap(cacheKey, fresh);
        if (!_sameJsonMap(fresh, cached)) {
          ref.invalidateSelf();
        }
      } catch (_) {}
    }());
    return cached;
  }

  try {
    final fresh = await fetch();
    await OfflineStore.saveMap(cacheKey, fresh);
    return fresh;
  } catch (e) {
    if (cached != null && cached.isNotEmpty) return cached;
    rethrow;
  }
}
