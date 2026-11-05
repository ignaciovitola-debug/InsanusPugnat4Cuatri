namespace GladiusAI
{
    // Los datos de un item tal como los muestra la tienda.
    public readonly struct ShopOffer
    {
        public readonly string name;
        public readonly string description;
        public readonly int price;

        public ShopOffer(string name, string description, int price)
        {
            this.name = name;
            this.description = description;
            this.price = price;
        }
    }
}
