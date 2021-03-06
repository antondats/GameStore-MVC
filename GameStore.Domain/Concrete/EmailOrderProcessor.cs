﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameStore.Domain.Abstract;
using GameStore.Domain.Entities;
using System.Net;
using System.Net.Mail;

namespace GameStore.Domain.Concrete
{
    public class EmailSetting
    {
        public string MailToAddress = "orders@example.com";
        public string MailFromAddress = "gamestore@example.com";
        public bool UseSsl = true;
        public string Username = "MySmtpUsername";
        public string Password = "MySmtpPassword";
        public string ServerName = "smtp.example.com";
        public int ServerPort = 587;
        public bool WriteAsFile = true;
        public string FileLocation = @"C:\Users\Антон\Documents\Visual Studio 2015\Projects\GameStore\mails";
    }

    public class EmailOrderProcessor : IOrderProcessor
    {
        private EmailSetting emailSettings;

        public EmailOrderProcessor(EmailSetting settings)
        {
            emailSettings = settings;
        }

        public void ProcessOrder(Cart cart, ShippingDetails shippingInfo)
        {
            using (var smtpClient = new SmtpClient())
            {
                smtpClient.EnableSsl = emailSettings.UseSsl;
                smtpClient.Host = emailSettings.ServerName;
                smtpClient.Port = emailSettings.ServerPort;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials
                    = new NetworkCredential(emailSettings.Username, emailSettings.Password);

                if (emailSettings.WriteAsFile)
                {
                    smtpClient.DeliveryMethod
                        = SmtpDeliveryMethod.SpecifiedPickupDirectory;
                    smtpClient.PickupDirectoryLocation = emailSettings.FileLocation;
                    smtpClient.EnableSsl = false;
                }

                StringBuilder body = new StringBuilder()
                   .AppendLine("Новый заказ обработан")
                   .AppendLine("---")
                   .AppendLine("Товары:");


                foreach (var line in cart.Lines)
                {
                    var subtotal = line.Game.Price * line.Quantity;
                    body.AppendFormat("{0} x {1} (итого: {2:c}",
                        line.Quantity, line.Game.Name, subtotal);
                }

                body.AppendFormat("Общая стоимость: {0:c}", cart.ComputeTotalValue())
                   .AppendLine("---")
                   .AppendLine("Доставка:")
                   .AppendLine(shippingInfo.Name)
                   .AppendLine(shippingInfo.Line1)
                   .AppendLine(shippingInfo.Line2 ?? "")
                   .AppendLine(shippingInfo.Line3 ?? "")
                   .AppendLine(shippingInfo.City)
                   .AppendLine(shippingInfo.Country)
                   .AppendLine("---")
                   .AppendFormat("Подарочная упаковка: {0}",
                       shippingInfo.GiftWrap ? "Да" : "Нет");

                MailMessage mailMessage = new MailMessage(
                                      emailSettings.MailFromAddress,    
                                      emailSettings.MailToAddress,     
                                      "Новый заказ отправлен!",    
                                      body.ToString());

                if (emailSettings.WriteAsFile)
                {
                    mailMessage.BodyEncoding = Encoding.UTF8;
                }

                smtpClient.Send(mailMessage);
            }
        }
    }
}
