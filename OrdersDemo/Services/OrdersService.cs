﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using ServiceStack.Common;
using ServiceStack.DataAnnotations;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

namespace OrdersDemo.Services
{
    [Route("/Orders", "GET")]
    public class Order : IReturn<OrdersResponse>
    {
        [AutoIncrement]
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; }
    }

    [Route("/Orders", "POST")]
    public class CreateOrder : IReturn<CreateOrderResponse>
    {
        public string CustomerName { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderResponse : IHasResponseStatus
    {
        public CreateOrderResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        public Order Order { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class OrdersResponse : IHasResponseStatus
    {
        public OrdersResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        public List<Order> Orders { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class OrdersService : Service
    {
        public IDbConnectionFactory dbConn { get; set; }
        public IRedisClientsManager redisClientManager { get; set; }
        public OrdersResponse Get(Order request)
        {
            using(var conn = dbConn.OpenDbConnection())
            {
                var orders = conn.Select<Order>();
                return new OrdersResponse {Orders = orders};
            }
        }

        public CreateOrderResponse Post(CreateOrder request)
        {
            using (var conn = dbConn.OpenDbConnection())
            {
                var newOrder = request.TranslateTo<Order>();
                newOrder.Status = "New";
                conn.Insert(newOrder);
                newOrder.Id = (int)conn.GetLastInsertId();
                //publish message
                using (var redisClient = redisClientManager.GetClient())
                {
                    redisClient.PublishMessage("NewOrder", request.ToJson());
                }
                return new CreateOrderResponse {Order = newOrder};
            }
        }
    }
}