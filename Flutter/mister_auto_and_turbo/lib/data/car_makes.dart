/// Car makes offered in the "Register Your Vehicle" dropdown.
const List<String> carMakes = [
  'Alfa Romeo',
  'Aston Martin',
  'Audi',
  'BMW',
  'BYD',
  'Bentley',
  'Cadillac',
  'Chery',
  'Chevrolet',
  'Citroën',
  'Fiat',
  'Ferrari',
  'Ford',
  'Genesis',
  'Geely',
  'GMC',
  'Great Wall Motors (GWM)',
  'Haval',
  'Honda',
  'Hyundai',
  'Infiniti',
  'Isuzu',
  'Jaguar',
  'Jeep',
  'Kia',
  'Lamborghini',
  'Land Rover',
  'Lexus',
  'Mahindra',
  'Maserati',
  'Mazda',
  'McLaren',
  'Mercedes-Benz',
  'MG',
  'Mini',
  'Mitsubishi',
  'Nissan',
  'Opel',
  'Peugeot',
  'Polestar',
  'Porsche',
  'Renault',
  'Rolls-Royce',
  'Škoda',
  'SEAT',
  'Subaru',
  'Suzuki',
  'Tesla',
  'Toyota',
  'Volkswagen',
  'Volvo',
];

String _norm(String s) => s
    .toLowerCase()
    .replaceAll('ë', 'e')
    .replaceAll('š', 's')
    .replaceAll('é', 'e')
    .trim();

/// Makes whose name (or any word in it) starts with [query]. Names that start
/// with the query come first. An empty query returns every make.
List<String> searchMakes(String query) {
  final q = _norm(query);
  if (q.isEmpty) return carMakes;
  final starts = <String>[];
  final wordStarts = <String>[];
  for (final m in carMakes) {
    final n = _norm(m);
    if (n.startsWith(q)) {
      starts.add(m);
    } else if (n.split(RegExp(r'[\s\-(]+')).any((w) => w.startsWith(q))) {
      wordStarts.add(m);
    }
  }
  return [...starts, ...wordStarts];
}

/// The exact make matching [text] (ignoring case / accents), or null.
String? exactMake(String text) {
  final q = _norm(text);
  for (final m in carMakes) {
    if (_norm(m) == q) return m;
  }
  return null;
}
