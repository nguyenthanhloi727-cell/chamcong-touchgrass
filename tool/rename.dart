// Doi ten project theo ho ten + MSSV (luat dat ten cua de).
//
//   dart run tool/rename.dart --name "Ho Ngoc Phu" --mssv 2380601699 [--dry-run]
//
// Chi dung dart:io. Ten hien tai duoc doc tu `name:` trong pubspec.yaml,
// ten file .csproj va android:label, khong ghi cung ten ai.
import 'dart:convert';
import 'dart:io';

const _skipDirs = {
  'bin', 'obj', 'build', '.git', '.dart_tool', '.gradle', '.idea', '.vs',
  'node_modules', 'TestResults', '.pub-cache', '.pub',
};

// File tai lieu cua repo goc, khong dong vao (chua ten thanh vien that).
const _skipFiles = {'CLAUDE.md', 'CLAUDE.local.md', 'PROGRESS.md', 'PLAN.md'};

const _textExt = {
  '.dart', '.cs', '.csproj', '.slnx', '.sln', '.json', '.yaml', '.yml', '.xml',
  '.kt', '.kts', '.gradle', '.md', '.http', '.properties', '.txt', '.props',
  '.targets', '.config', '.metadata', '.gitignore',
};

const _vi = {
  'a': 'àáạảãâầấậẩẫăằắặẳẵ',
  'e': 'èéẹẻẽêềếệểễ',
  'i': 'ìíịỉĩ',
  'o': 'òóọỏõôồốộổỗơờớợởỡ',
  'u': 'ùúụủũưừứựửữ',
  'y': 'ỳýỵỷỹ',
  'd': 'đ',
};

String _stripAccents(String s) {
  final map = <String, String>{};
  _vi.forEach((base, chars) {
    for (final c in chars.split('')) {
      map[c] = base;
      map[c.toUpperCase()] = base.toUpperCase();
    }
  });
  return s.split('').map((c) => map[c] ?? c).join();
}

class _Names {
  _Names(this.snake, this.pascal, this.label);
  final String snake; // ho_ngoc_phu_2380601699
  final String pascal; // HoNgocPhu_2380601699
  final String label; // Hồ Ngọc Phú - 2380601699
}

_Names _build(String name, String mssv) {
  final words = _stripAccents(name)
      .split(RegExp(r'\s+'))
      .where((w) => w.isNotEmpty)
      .map((w) => w.replaceAll(RegExp(r'[^A-Za-z0-9]'), ''))
      .where((w) => w.isNotEmpty)
      .toList();
  if (words.isEmpty) {
    stderr.writeln('Ten khong hop le: "$name"');
    exit(64);
  }
  final snake = '${words.map((w) => w.toLowerCase()).join('_')}_$mssv';
  final pascal =
      '${words.map((w) => w[0].toUpperCase() + w.substring(1).toLowerCase()).join()}_$mssv';
  return _Names(snake, pascal, '${name.trim().replaceAll(RegExp(r'\s+'), ' ')} - $mssv');
}

bool _isText(File f) {
  final n = f.uri.pathSegments.last;
  final dot = n.lastIndexOf('.');
  return _textExt.contains(dot < 0 ? n : n.substring(dot));
}

bool _skipPath(String root, String path) {
  final rel = path.substring(root.length).replaceAll('\\', '/');
  final parts = rel.split('/').where((p) => p.isNotEmpty).toList();
  for (var i = 0; i < parts.length - 1; i++) {
    if (_skipDirs.contains(parts[i])) return true;
  }
  if (parts.isNotEmpty && _skipDirs.contains(parts.last)) return true;
  return _skipFiles.contains(parts.last);
}

List<FileSystemEntity> _walk(String root) =>
    Directory(root).listSync(recursive: true, followLinks: false)
        .where((e) => !_skipPath(root, e.path))
        .toList();

bool _hasBom(List<int> b) => b.length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF;

// Doc UTF-8, bo BOM de so khop; _write them lai BOM neu file goc co.
String? _readOrNull(File f) {
  try {
    final bytes = f.readAsBytesSync();
    return utf8.decode(_hasBom(bytes) ? bytes.sublist(3) : bytes);
  } on FormatException {
    return null;
  }
}

void _write(File f, String text) {
  final bom = _hasBom(f.readAsBytesSync());
  f.writeAsBytesSync([if (bom) ...const [0xEF, 0xBB, 0xBF], ...utf8.encode(text)]);
}

void main(List<String> args) {
  String? name;
  String? mssv;
  var dry = false;
  for (var i = 0; i < args.length; i++) {
    switch (args[i]) {
      case '--name':
        name = i + 1 < args.length ? args[++i] : null;
      case '--mssv':
        mssv = i + 1 < args.length ? args[++i] : null;
      case '--dry-run':
        dry = true;
      default:
        stderr.writeln('Tham so la: ${args[i]}');
        exit(64);
    }
  }
  if (name == null || mssv == null || !RegExp(r'^\d+$').hasMatch(mssv)) {
    stderr.writeln(
        'Dung: dart run tool/rename.dart --name "Ho Ten" --mssv 2380601699 [--dry-run]');
    exit(64);
  }

  final sep = Platform.pathSeparator;
  final scriptPath = File.fromUri(Platform.script).absolute.path;
  final root = File(scriptPath).parent.parent.path; // tool/ -> goc repo
  final next = _build(name, mssv);

  // 17.2 doc ten hien tai
  final mobileDirs = Directory('$root${sep}mobile').existsSync()
      ? Directory('$root${sep}mobile').listSync().whereType<Directory>().where(
          (d) => File('${d.path}${sep}pubspec.yaml').existsSync()).toList()
      : <Directory>[];
  if (mobileDirs.length != 1) {
    stderr.writeln('Can dung 1 project Flutter trong mobile/, tim thay ${mobileDirs.length}');
    exit(1);
  }
  final pubspec = File('${mobileDirs.single.path}${sep}pubspec.yaml');
  final oldSnake = RegExp(r'^name:\s*(\S+)', multiLine: true)
      .firstMatch(pubspec.readAsStringSync())
      ?.group(1);
  final csprojs = Directory('$root${sep}backend')
      .listSync(recursive: true)
      .whereType<File>()
      .where((f) => !_skipPath(root, f.path) && f.path.endsWith('.ChamCong.Api.csproj'))
      .toList();
  if (oldSnake == null || csprojs.length != 1) {
    stderr.writeln('Khong doc duoc ten hien tai (pubspec name / .ChamCong.Api.csproj)');
    exit(1);
  }
  final csName = csprojs.single.uri.pathSegments.last;
  final oldPascal = csName.substring(0, csName.length - '.ChamCong.Api.csproj'.length);
  final manifest = File(
      '${mobileDirs.single.path}${sep}android${sep}app${sep}src${sep}main${sep}AndroidManifest.xml');
  final oldLabel = RegExp(r'android:label="([^"]*)"')
      .firstMatch(manifest.readAsStringSync())
      ?.group(1);

  print('Ten cu : $oldSnake | $oldPascal | $oldLabel');
  print('Ten moi: ${next.snake} | ${next.pascal} | ${next.label}${dry ? '   [dry-run]' : ''}');

  // Thu tu thay: chuoi dai/cu the truoc.
  final pairs = <List<String>>[
    if (oldLabel != null && oldLabel.isNotEmpty) [oldLabel, next.label],
    [oldPascal, next.pascal],
    [oldSnake, next.snake],
  ];

  // 17.3, 17.4 (noi dung), 17.6 (namespace/using/csproj/slnx): thay trong file van ban
  var changedFiles = 0;
  for (final e in _walk(root)) {
    if (e is! File || !_isText(e)) continue;
    final text = _readOrNull(e);
    if (text == null) continue;
    var out = text;
    for (final p in pairs) {
      out = out.replaceAll(p[0], p[1]);
    }
    if (out == text) continue;
    changedFiles++;
    print('  sua  ${e.path.substring(root.length + 1)}');
    if (!dry) _write(e, out);
  }

  // 17.4 (thu muc kotlin), 17.5, 17.6: doi ten thu muc/file, sau cung truoc
  final renames = _walk(root)
      .where((e) {
        final n = e.uri.pathSegments.where((s) => s.isNotEmpty).last;
        return n.contains(oldSnake) || n.contains(oldPascal);
      })
      .toList()
    ..sort((a, b) => b.path.length.compareTo(a.path.length)); // sau truoc
  var renamed = 0;
  for (final e in renames) {
    final base = e.uri.pathSegments.where((s) => s.isNotEmpty).last;
    final to = base.replaceAll(oldPascal, next.pascal).replaceAll(oldSnake, next.snake);
    if (to == base) continue;
    renamed++;
    final dest = '${e.parent.path}$sep$to';
    print('  doi  ${e.path.substring(root.length + 1)} -> $to');
    if (dry) continue;
    try {
      if (e is Directory) {
        e.renameSync(dest);
      } else {
        (e as File).renameSync(dest);
      }
    } on FileSystemException catch (err) {
      stderr.writeln('\nKhong doi ten duoc ${e.path}: ${err.osError?.message ?? err.message}.\n'
          'Thuong do Gradle/IDE dang giu thu muc: chay `gradlew --stop`, dong IDE/terminal '
          'dang mo trong thu muc do, roi doi tay thu muc thanh $to.');
      exit(1);
    }
  }

  // 17.7 quet ten cu con sot
  var left = 0;
  if (dry) {
    print('\n[dry-run] se sua $changedFiles file, doi $renamed muc. Khong ghi gi.');
    return;
  }
  final needles = [oldSnake, oldPascal, if (oldLabel != null && oldLabel.isNotEmpty) oldLabel]
      .where((s) => s != next.snake && s != next.pascal && s != next.label)
      .map((s) => s.toLowerCase())
      .toList();
  for (final e in _walk(root)) {
    final rel = e.path.substring(root.length + 1);
    if (needles.any((n) => rel.toLowerCase().contains(n))) {
      left++;
      print('  SOT (ten) $rel');
    }
    if (e is! File || !_isText(e)) continue;
    final text = _readOrNull(e)?.toLowerCase();
    if (text == null) continue;
    for (final n in needles) {
      var idx = text.indexOf(n);
      while (idx >= 0) {
        left++;
        final line = '\n'.allMatches(text.substring(0, idx)).length + 1;
        print('  SOT $rel:$line');
        idx = text.indexOf(n, idx + n.length);
      }
    }
  }
  print('\nDa sua $changedFiles file, doi $renamed muc. Con sot ten cu: $left');
  if (left > 0) exit(1);
  print('Tiep theo: cd mobile/${next.snake} && flutter clean && flutter pub get');
}
