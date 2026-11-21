namespace GladiusAI
{
    // Los datos de un item tal como los muestra la tienda.
    public readonly struct ShopOffer
    {
        public readonly string name;
        public readonly string description;
        public readonly int price;
        public readonly string priceText;

        public ShopOffer(string name, string description, int price, string priceText)
        {
            this.name = name;
            this.description = description;
            this.price = price;
            this.priceText = priceText;
        }
    }
}
