using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameStore.Domain.Entities
{
    public class Cart
    {
        private List<CartLine> lineCollection = new List<CartLine>();

        public IEnumerable<CartLine> Lines
        {
            get { return lineCollection; }
        }

        public void AddItem(Game game, int quantity)
        {
            CartLine line = lineCollection.Where(crt => crt.Game.GameId == game.GameId).FirstOrDefault();
            if(line == null)
            {
                lineCollection.Add(new CartLine
                {
                    Game = game,
                    Quantity = quantity
                });
            }
            else
            {
                line.Quantity += quantity;
            }
        }

        public void RemoveLine(Game game)
        {
            lineCollection.RemoveAll(l => l.Game.GameId == game.GameId);
        }

        public decimal ComputeTotalValue()
        {
            return lineCollection.Sum(l => l.Game.Price * l.Quantity);
        }

        public void Clear()
        {
            lineCollection.Clear();
        }
    }

    public class CartLine
    {
        public Game Game { get; set; }
        public int Quantity { get; set; }
    }
}
