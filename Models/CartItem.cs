namespace INF4027W_BPTTIN002_MiniPrj_2026.Models
{
    public class CartItem
    {
            public string ProductId { get; set; }
            public string ProductName { get; set; } 
            public double Price { get; set; }
            public int Quantity { get; set; }
            public string ImageUrl { get; set; }
            public string? SelectedSize { get; set; }

    }
}

