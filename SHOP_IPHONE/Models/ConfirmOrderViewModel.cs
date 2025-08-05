using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SHOP_IPHONE.Models
{
    public class ConfirmOrderViewModel
    {
        public List<CartItemModel> CartItems { get; set; }
        public int OrderId { get; set; }

        public decimal ShippingFee { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string SelectedPaymentMethod { get; set; }

        public List<string> PaymentMethods { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string Address { get; set; }
        public List<Address> Adresses { get; set; }
        public int? SelectedAddressId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string Phone { get; set; }

        // ✅ Xác nhận chuyển khoản nếu chọn hình thức chuyển khoản
        public bool ConfirmedTransfer { get; set; }

        public decimal Subtotal => CartItems?.Sum(x => x.Total) ?? 0;
        [Display(Name = "Tôi đã chuyển khoản")]
        public bool HasTransferred { get; set; }
        public ConfirmOrderViewModel()
        {
            CartItems = new List<CartItemModel>();
            PaymentMethods = new List<string>();
        }
        public List<Voucher> Vouchers { get; set; }
        public int? SelectedVoucherId { get; set; }
        public List<VoucherViewModel> AvailableVouchers { get; set; }

        public VoucherViewModel SelectedVoucher =>
            AvailableVouchers?.FirstOrDefault(v => v.voucher_id == SelectedVoucherId);
        public decimal DiscountAmount { get; set; } // Nếu chưa có, hãy thêm dòng này
    }
}