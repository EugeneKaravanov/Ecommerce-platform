using FluentValidation;
using OrderService.Models;

namespace OrderService.Validators
{
    public class OrderValidator : AbstractValidator<InputOrder>
    {
        private readonly int minOrderItems = 1;
        private readonly int minOrderItemQuantity = 1;

        public OrderValidator()
        {
            RuleFor(order => order.CustomerId).NotEmpty();
            RuleFor(order => order.OrderItems.Count).GreaterThanOrEqualTo(minOrderItems);
            RuleForEach(order => order.OrderItems).ChildRules(orderItem =>
            {
                orderItem.RuleFor(orderItem => orderItem.Quantity).GreaterThanOrEqualTo(minOrderItemQuantity);
            });
        }
    }
}
