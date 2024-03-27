﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;
using weSell.DataAccess.Repository.IRepository;
using weSell.Models;
using weSell.Models.ViewModels;
using weSell.Utility;

namespace weSellWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == UserId, includeProperties: "Product"),
               OrderHeader = new()
            };

            IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }

            return View(ShoppingCartVM);
        }

        public IActionResult plus(int cartId)
        {
			
			var carFromDb = _unitOfWork.ShoppingCart.Get(u=>u.Id == cartId);
            carFromDb.Count += 1;
            _unitOfWork.ShoppingCart.Update(carFromDb);
            _unitOfWork.Save();
			
			return RedirectToAction(nameof(Index));

        }

        public IActionResult minus(int cartId)
        {
            var carFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if(carFromDb.Count <= 1)
            {
                _unitOfWork.ShoppingCart.Remove(carFromDb);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == carFromDb.ApplicationUserId).Count() - 1);

            } else
            {
                carFromDb.Count -= 1;
                _unitOfWork.ShoppingCart.Update(carFromDb);
            }
            
            
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        


        public IActionResult remove(int cartId)
        {
            var carFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
            _unitOfWork.ShoppingCart.Remove(carFromDb);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                    .GetAll(u => u.ApplicationUserId == carFromDb.ApplicationUserId).Count() - 1);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary() 
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var UserId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
			
			ShoppingCartVM = new()
            {
                ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == UserId, includeProperties: "Product"),
                OrderHeader = new()
            };
            ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == UserId);
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                cart.Price = GetPriceBasedOnQuantity(cart);
                ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
            }
            return View(ShoppingCartVM);
        }


        [HttpPost]
        [ActionName("Summary")]
		public IActionResult SummaryPost()
		{


			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

			ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
				includeProperties: "Product");

			ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
			ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

			ApplicationUser applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);


			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
			}


			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//it is a regular customer 
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
			}
			else
			{
				//it is a company user
				ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
			}
			_unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
			_unitOfWork.Save();

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
					Price = cart.Price,
					Count = cart.Count
				};
				_unitOfWork.OrderDetail.Add(orderDetail);
				_unitOfWork.Save();
			}
			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				//it is a regular customer account and we need to capture payment
				//stripe logic
				var domain = Request.Scheme + "://" + Request.Host.Value + "/";
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
					CancelUrl = domain + "customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in ShoppingCartVM.ShoppingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
							Currency = "zar",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}


				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);

			}


			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
		}

		public IActionResult OrderConfirmation(int id) 
        {
			OrderHeader orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}

                HttpContext.Session.Clear();

            }
			List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
			   .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

			_unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
			_unitOfWork.Save();
			return View();
        }


		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;

            }
            else
            {
                if(shoppingCart.Count <= 100)
                {
                    return shoppingCart.Product.Price50;
                }
                else
                {
                    return shoppingCart.Product.Price50;
                }
            }

        }
    }

    
}