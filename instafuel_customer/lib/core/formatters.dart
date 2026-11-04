import 'package:intl/intl.dart';

final _money = NumberFormat.currency(symbol: '৳', decimalDigits: 2);
final _number = NumberFormat('#,##0.##');

String formatMoney(double amount) => _money.format(amount);
String formatLitres(double litres) => '${_number.format(litres)} L';

String formatDateTime(DateTime dt) => DateFormat('d MMM yyyy, h:mm a').format(dt);
String formatDate(DateTime dt) => DateFormat('EEE, d MMM').format(dt);
String formatTime(DateTime dt) => DateFormat('h:mm a').format(dt);

/// "in 35 min", "in 2 days", "3 days ago"
String formatRelative(DateTime dt) {
  final diff = dt.difference(DateTime.now());
  final past = diff.isNegative;
  final d = diff.abs();
  final text = d.inDays >= 1
      ? '${d.inDays} day${d.inDays == 1 ? '' : 's'}'
      : d.inHours >= 1
          ? '${d.inHours} h'
          : '${d.inMinutes} min';
  return past ? '$text ago' : 'in $text';
}
