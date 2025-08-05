using SHOP_IPHONE.Helpers;
using SHOP_IPHONE.Models;
using SHOP_IPHONE.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace SHOP_IPHONE.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        private DBiphoneEntities1 db = new DBiphoneEntities1();
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            // Xóa các tài khoản tự khóa quá 1 giờ
            DeleteExpiredAccounts();

            var account = db.Accounts.FirstOrDefault(a => a.username == username && a.password == password);

            if (account == null)
            {
                ViewBag.ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
                return View();
            }

            // Bảo vệ tài khoản Admin: không bao giờ bị khóa
            if (account.idtype == 2)
            {
                if (account.isLocked || account.isLockedByAdmin)
                {
                    account.isLocked = false;
                    account.isLockedByAdmin = false;
                    account.lockDate = null;
                    db.SaveChanges();
                }
            }

            // Nếu bị khóa bởi Admin
            if (account.isLockedByAdmin)
            {
                ViewBag.ErrorMessage = "Tài khoản đã bị khóa bởi Admin.";
                return View();
            }

            // Nếu tự khóa
            if (account.isLocked)
            {
                var oneHourAgo = DateTime.Now.AddHours(-1);
                if (account.lockDate != null && account.lockDate < oneHourAgo)
                {
                    var addresses = db.Addresses.Where(a => a.AccountId == account.account_id).ToList();
                    db.Addresses.RemoveRange(addresses);
                    db.Accounts.Remove(account);
                    db.SaveChanges();
                    ViewBag.ErrorMessage = "Tài khoản đã bị xóa vì không mở lại trong 1 giờ.";
                    return View();
                }

                // Nếu chưa quá 1 giờ → cho phép vào UserProfile để mở
                Session["account_id"] = account.account_id;
                Session["username"] = account.username;
                Session["role"] = account.idtype;
                TempData["Info"] = "Tài khoản của bạn đang tạm khóa. Vui lòng mở khóa để tiếp tục sử dụng.";
                return RedirectToAction("UserProfile", "Account");
            }

            // Đăng nhập thành công bình thường
            Session["account_id"] = account.account_id;
            Session["username"] = account.username;
            Session["role"] = account.idtype;

            return RedirectToAction("Index", "Product");
        }



        public ActionResult Register()
        {
            ViewBag.IdTypes = new SelectList(new[] {
    new { Id = 1, Name = "Người dùng" },
    new { Id = 2, Name = "Admin" }
}, "Id", "Name");

            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var existing = db.Accounts.FirstOrDefault(a => a.email == model.email || a.username == model.username);
            if (existing != null)
            {
                ViewBag.ErrorMessage = "Email hoặc tên đăng nhập đã tồn tại.";
                return View(model);
            }

            Account newAccount = new Account
            {
                full_name = model.FullName,
                username = model.username,
                email = model.email,
                password = model.password,
                phone = model.phone,
                address = model.address,
                idtype = (int)AccountType.User
            };

            db.Accounts.Add(newAccount);
            db.SaveChanges();

            return RedirectToAction("Login");
        }
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Index", "Product");
        }
        public ActionResult Delete(int id)
        {
            var account = db.Accounts.Find(id);
            if (account == null)
            {
                return HttpNotFound();
            }
            db.Accounts.Remove(account);
            db.SaveChanges();
            return RedirectToAction("Login");
        }
        public ActionResult UserProfile()
        {
            int id = (int)Session["account_id"];
            var acc = db.Accounts.Find(id);
            return View(acc);
        }
        [HttpGet]
        public ActionResult EditProfile()
        {
            int accountId = (int)Session["account_id"];
            var acc = db.Accounts.Find(accountId);

            var model = new EditProfileViewModel
            {
                account_id = acc.account_id,
                username = acc.username,
                email = acc.email,
                phone = acc.phone,
                address = acc.address,
                fullname = acc.full_name,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var acc = db.Accounts.Find(model.account_id);
            if (acc == null) return RedirectToAction("Login");

            // Nếu email thay đổi => yêu cầu xác thực lại
            if (acc.email != model.email)
            {
                string otp = GenerateOtp();
                Session["OTP"] = otp;
                Session["VerifyUserId"] = acc.account_id;
                acc.email = model.email;
                acc.isVerified = false;
                TempData["Info"] = "Email đã thay đổi. Vui lòng xác thực lại.";
            }

            // Cập nhật thông tin cá nhân khác
            acc.username = model.username;
            acc.phone = model.phone;
            acc.address = model.address;
            acc.full_name = model.fullname;

            // Nếu người dùng đổi mật khẩu
            if (!string.IsNullOrEmpty(model.CurrentPassword) ||
                !string.IsNullOrEmpty(model.NewPassword) ||
                !string.IsNullOrEmpty(model.ConfirmPassword))
            {
                if (model.CurrentPassword != acc.password)
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.NewPassword) || string.IsNullOrEmpty(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin đổi mật khẩu");
                    return View(model);
                }
                string subject = "Thay đổi mật khẩu thành công";
                string body = $"<p>Xin chào {acc.full_name},</p><p>Bạn đã đổi mật khẩu thành công.</p>";

                MailHelper.SendEmail(acc.email, subject, body);

                acc.password = model.NewPassword;
            }

            db.SaveChanges();
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("UserProfile", "Account");
        }
        public ActionResult ForgotPassword()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var account = db.Accounts.FirstOrDefault(a => a.email == model.EmailOrPhone);
            if (account == null)
            {
                ViewBag.Error = "Email không đúng";
                return View(model);
            }

            string otp = new Random().Next(100000, 999999).ToString();
            Session["ResetOTP"] = otp;
            Session["ResetUserId"] = account.account_id;

            // Gửi OTP qua email
            string subject = "Yêu cầu đặt lại mật khẩu";
            string body = $"<p>Xin chào {account.full_name},</p>" +
                          $"<p>Mã OTP để đặt lại mật khẩu của bạn là: <strong>{otp}</strong></p>" +
                          "<p>Không chia sẻ mã này với bất kỳ ai.</p>";

            MailHelper.SendEmail(account.email, subject, body);

            TempData["Info"] = "Mã OTP đã được gửi tới email";
            return RedirectToAction("ConfirmResetOtp");
        }
        [HttpGet]
        public ActionResult ConfirmResetOtp()
        {
            ViewBag.Info = TempData["Info"];
            ViewBag.Error = TempData["Error"];
            return View();
        }

        [HttpPost]
        public ActionResult ConfirmResetOtp(string otp)
        {
            if (Session["ResetOTP"] != null && otp == Session["ResetOTP"].ToString())
            {
                Session["ResetOtpVerified"] = true;
                return RedirectToAction("ResetPassword");
            }

            TempData["Error"] = "Mã OTP không chính xác";
            return RedirectToAction("ConfirmResetOtp");
        }
        [HttpGet]
        public ActionResult ResetPassword()
        {
            if (Session["ResetOtpVerified"] == null)
            {
                TempData["Error"] = "Vui lòng xác nhận OTP trước";
                return RedirectToAction("ForgotPassword");
            }

            return View(new ResetPasswordViewModel());
        }

        [HttpPost]
        public ActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (Session["ResetUserId"] == null)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn hoặc không hợp lệ";
                return RedirectToAction("ForgotPassword", "Account");
            }

            int userId = (int)Session["ResetUserId"];
            var acc = db.Accounts.Find(userId);
            if (acc != null)
            {
                acc.password = model.NewPassword;
                db.SaveChanges();

                // Gửi email xác nhận đã đổi mật khẩu
                string subject = "Đổi mật khẩu thành công";
                string body = $"<p>Chào {acc.full_name},</p>" +
                              "<p>Bạn đã đổi mật khẩu thành công.</p>";

                MailHelper.SendEmail(acc.email, subject, body);

                // Xóa session tạm
                Session.Remove("ResetOTP");
                Session.Remove("ResetUserId");
                Session.Remove("ResetOtpVerified");

                TempData["Success"] = "Mật khẩu đã được cập nhật thành công";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", "Tài khoản không hợp lệ");
            return View(model);
        }



        private string GenerateOtp(int length = 6)
        {
            var random = new Random();
            var otp = new StringBuilder();
            for (int i = 0; i < length; i++)
                otp.Append(random.Next(0, 10)); // Tạo chuỗi số ngẫu nhiên
            return otp.ToString();
        }

        [HttpGet]
        public ActionResult VerifyOTP()
        {
            return View();
        }

        [HttpPost]
        public ActionResult VerifyOTP(string otp)
        {
            if (string.IsNullOrEmpty(otp))
            {
                ViewBag.Error = "Vui lòng nhập mã xác thực";
                return View();
            }

            if (Session["OTP"] == null || Session["ResetUserId"] == null)
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng thử lại.";
                return RedirectToAction("ForgotPassword");
            }

            if (otp == Session["OTP"].ToString())
            {
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Mã xác nhận không chính xác.";
            return View();
        }

        public ActionResult SendVerificationEmail()
        {
            int accountId = (int)Session["account_id"];
            var acc = db.Accounts.Find(accountId);

            if (acc == null)
            {
                TempData["Error"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("Login");
            }

            // Tạo OTP và lưu vào Session
            string otp = new Random().Next(100000, 999999).ToString();
            Session["OTP"] = otp;
            Session["VerifyUserId"] = acc.account_id;

            // Gửi OTP qua email
            string subject = "Xác thực email";
            string body = $"<p>Chào {acc.full_name},</p>" +
                          $"<p>Mã OTP của bạn là: <strong>{otp}</strong></p>" +
                          "<p>Vui lòng nhập mã này để xác thực tài khoản.</p>";

            MailHelper.SendEmail(acc.email, subject, body);

            return RedirectToAction("VerifyEmail");
        }

        [HttpGet]
        public ActionResult VerifyEmail()
        {
            ViewBag.Info = TempData["Info"];
            ViewBag.Error = TempData["Error"];
            ViewBag.Success = TempData["Success"];
            return View();
        }

        [HttpPost]
        public ActionResult VerifyEmail(string otp)
        {
            if (Session["OTP"] != null && Session["VerifyUserId"] != null)
            {
                if (otp == Session["OTP"].ToString())
                {
                    int id = (int)Session["VerifyUserId"];
                    var user = db.Accounts.Find(id);

                    if (user != null)
                    {
                        user.isVerified = true;
                        db.SaveChanges();
                        string subject = "Xác thực email thành công";
                        string body = $"<p>Xin chào {user.full_name},</p>" +
                                      "<p>Bạn đã xác thực địa chỉ email thành công.</p>" +
                                      "<p>Hệ thống đã ghi nhận thông tin này.</p>";

                        MailHelper.SendEmail(user.email, subject, body);
                        Session.Remove("OTP");
                        Session.Remove("VerifyUserId");

                        TempData["Success"] = "Email đã được xác thực thành công✅";
                        return RedirectToAction("VerifyEmail");
                    }
                }
                else
                {
                    TempData["Error"] = "Mã OTP không đúng, vui lòng thử lại❌";
                    return RedirectToAction("VerifyEmail");
                }
            }
            TempData["Error"] = "Phiên xác thực đã hết hạn, vui lòng gửi lại mã xác thực⚠️";
            return RedirectToAction("SendVerificationEmail");
        }

        public ActionResult LockMyAccount()
        {
            int id = (int)Session["account_id"];
            var acc = db.Accounts.Find(id);
            if (acc != null)
            {
                acc.isLocked = true;
                acc.lockDate = DateTime.Now;
                db.SaveChanges();
                MailHelper.SendEmail(acc.email, "Tài khoản đã khóa", "Bạn đã khóa tài khoản thành công. Bạn có thể mở lại trong vòng 1 ngày.");
                TempData["Info"] = "Tài khoản đã được khóa. Bạn có thể mở lại trong vòng 1 ngày.";
                Session.Clear();
            }
            return RedirectToAction("Login");
        }
        public ActionResult UnlockMyAccount()
        {
            int id = (int)Session["account_id"];
            var acc = db.Accounts.Find(id);
            if (acc != null && acc.isLocked)
            {
                acc.isLocked = false;
                acc.lockDate = null;
                db.SaveChanges();
                MailHelper.SendEmail(acc.email, "Tài khoản mở lại", "Bạn đã mở lại tài khoản thành công");
                TempData["Success"] = "Đã mở khóa";
            }
            return RedirectToAction("UserProfile", "Account");
        }
        private void DeleteExpiredAccounts()
        {
            var oneHourAgo = DateTime.Now.AddHours(-1);

            var expired = db.Accounts
                .Where(a => a.isLocked && !a.isLockedByAdmin && a.lockDate < oneHourAgo)
                .ToList();

            if (expired.Any())
            {
                foreach (var acc in expired)
                {
                    var addresses = db.Addresses.Where(a => a.AccountId == acc.account_id).ToList();
                    db.Addresses.RemoveRange(addresses);
                    db.Accounts.Remove(acc);
                }
                db.SaveChanges();
                TempData["Info"] = "Tài khoản của bạn đã bị xóa vĩnh viễn vì đã khóa hơn 1 giờ mà không mở lại.";
            }
        }
    }
}