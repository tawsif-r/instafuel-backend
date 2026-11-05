// Fuel products the customer can order. Prices come from the repository
// (centrally managed on the backend later), never hard-coded in screens.

enum FuelType {
  octane('Octane'),
  petrol('Petrol'),
  diesel('Diesel');

  const FuelType(this.label);
  final String label;
}

class FuelProduct {
  const FuelProduct({required this.type, required this.pricePerLitre});

  final FuelType type;
  final double pricePerLitre;
}
