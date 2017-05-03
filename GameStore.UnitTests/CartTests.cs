using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Collections.Generic;
using GameStore.Domain.Entities;
using Moq;
using GameStore.Domain.Abstract;
using GameStore.WebUI.Controllers;
using System.Web.Mvc;
using GameStore.WebUI.Models;

namespace GameStore.UnitTests
{
    [TestClass]
    public class CartTests
    {
        [TestMethod]
        public void Can_Add_New_Lines()
        {
            //Arrange
            Game game1 = new Game { GameId = 1, Name = "Игра1" };
            Game game2 = new Game { GameId = 2, Name = "Игра2" };

            Cart cart = new Cart();

            //Action
            cart.AddItem(game1, 1);
            cart.AddItem(game2, 1);
            List<CartLine> results = cart.Lines.ToList();

            //Assert
            Assert.AreEqual(results.Count(), 2);
            Assert.AreEqual(results[0].Game, game1);
            Assert.AreEqual(results[1].Game, game2);
        }

        [TestMethod]
        public void Can_Add_Quantity_For_Existing_Lines()
        {
            //Arrange
            Game game1 = new Game { GameId = 1, Name = "Игра1" };
            Game game2 = new Game { GameId = 2, Name = "Игра2" };

            Cart cart = new Cart();

            //Action
            cart.AddItem(game1, 1);
            cart.AddItem(game2, 1);
            cart.AddItem(game1, 5);
            List<CartLine> results = cart.Lines.OrderBy(c => c.Game.GameId).ToList();

            //Assert
            Assert.AreEqual(results.Count(), 2);
            Assert.AreEqual(results[0].Quantity, 6);
            Assert.AreEqual(results[1].Quantity, 1);
        }

        [TestMethod]
        public void Can_Remove_Line()
        {
            //Arrange
            Game game1 = new Game { GameId = 1, Name = "Игра1" };
            Game game2 = new Game { GameId = 2, Name = "Игра2" };
            Game game3 = new Game { GameId = 3, Name = "Игра3" };

            Cart cart = new Cart();

            //Action
            cart.AddItem(game1, 1);
            cart.AddItem(game2, 4);
            cart.AddItem(game3, 2);
            cart.AddItem(game2, 1);
            cart.RemoveLine(game2);

            //Assert
            Assert.AreEqual(cart.Lines.Where(c => c.Game == game2).Count(), 0);
            Assert.AreEqual(cart.Lines.Count(), 2);
        }

        [TestMethod]
        public void Calculate_Cart_Total()
        {
            //Arrange
            Game game1 = new Game { GameId = 1, Name = "Игра1", Price = 100 };
            Game game2 = new Game { GameId = 2, Name = "Игра2", Price = 55 };

            Cart cart = new Cart();

            //Action
            cart.AddItem(game1, 1);
            cart.AddItem(game2, 1);
            cart.AddItem(game1, 5);
            decimal result = cart.ComputeTotalValue();

            //Asserrt
            Assert.AreEqual(result, 655);
        }

        [TestMethod]
        public void Can_Clear_Contents()
        {
            //Arrange
            Game game1 = new Game { GameId = 1, Name = "Игра1", Price = 100 };
            Game game2 = new Game { GameId = 2, Name = "Игра2", Price = 55 };

            Cart cart = new Cart();

            //Action
            cart.AddItem(game1, 1);
            cart.AddItem(game2, 1);
            cart.AddItem(game1, 5);
            cart.Clear();

            //Assert
            Assert.AreEqual(cart.Lines.Count(), 0);
        }

        [TestMethod]
        public void Can_Add_To_Cart()
        {
            //Arrange
            Mock<IGameRepository> mock = new Mock<IGameRepository>();
            mock.Setup(m => m.Games).Returns(new List<Game> {
             new Game {GameId = 1, Name = "Игра1", Category = "Кат1"},
            }.AsQueryable());

            Cart cart = new Cart();

            CartController controller = new CartController(mock.Object);

            //Action
            controller.AddToCart(cart, 1, null);

            //Assert
            Assert.AreEqual(cart.Lines.Count(), 1);
            Assert.AreEqual(cart.Lines.ToList()[0].Game.GameId, 1);
        }

        [TestMethod]
        public void Adding_Game_To_Cart_Goes_To_Cart_Screen()
        {
            //Arrange
            Mock<IGameRepository> mock = new Mock<IGameRepository>();
            mock.Setup(m => m.Games).Returns(new List<Game> {
            new Game {GameId = 1, Name = "Игра1", Category = "Кат1"},
                }.AsQueryable());

            Cart cart = new Cart();

            CartController controller = new CartController(mock.Object);

            //Action
            RedirectToRouteResult result = controller.AddToCart(cart, 2, "myUrl");

            //Assert
            Assert.AreEqual(result.RouteValues["action"], "Index");
            Assert.AreEqual(result.RouteValues["returnUrl"], "myUrl");
        }

        [TestMethod]
        public void Can_View_Cart_Contents()
        {
            //Arrange
            Cart cart = new Cart();

            CartController target = new CartController(null);

            //Action
            CartIndexViewModel result
                = (CartIndexViewModel)target.Index(cart, "myUrl").ViewData.Model;

            //Assert
            Assert.AreSame(result.Cart, cart);
            Assert.AreEqual(result.ReturnUrl, "myUrl");
        }

        [TestMethod]
        public void Cannot_Checkout_Empty_Cart()
        {
            //Arrange
            Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();

            Cart cart = new Cart();

            ShippingDetails shippingDetails = new ShippingDetails();

            CartController controller = new CartController(null, mock.Object);

            //Action
            ViewResult result = controller.Checkout(cart, shippingDetails);

            //Assert
            mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(), It.IsAny<ShippingDetails>()),
                Times.Never());
            Assert.AreEqual("", result.ViewName);
            Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
        }

        [TestMethod]
        public void Cannot_Checkout_Invalid_ShippingDetails()
        {
            //Arrange
            Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();

            Cart cart = new Cart();
            cart.AddItem(new Game(), 1);

            CartController controller = new CartController(null, mock.Object);

            controller.ModelState.AddModelError("error", "error");

            //Action
            ViewResult result = controller.Checkout(cart, new ShippingDetails());

            //Assert
            mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(), It.IsAny<ShippingDetails>()),
                Times.Never());
            Assert.AreEqual("", result.ViewName);
            Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
        }

        [TestMethod]
        public void Can_Checkout_And_Submit_Order()
        {
            //Arrange
            Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();

            Cart cart = new Cart();
            cart.AddItem(new Game(), 1);

            CartController controller = new CartController(null, mock.Object);

            //Action
            ViewResult result = controller.Checkout(cart, new ShippingDetails());

            //Assert
            mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(), It.IsAny<ShippingDetails>()),
                Times.Once());
            Assert.AreEqual("Completed", result.ViewName);
            Assert.AreEqual(true, result.ViewData.ModelState.IsValid);
        }
    }
}
