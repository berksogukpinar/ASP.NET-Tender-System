using IhaleProject.Models;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace IhaleProject.Controllers
{
    public class HomeController : Controller
    {
        #region[Id ve Token Bilgisinin Girildiği Sayfa]
        /// <summary>
        /// İhale Projesi Giriş Sayfası
        /// </summary>
        /// <param name="token"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult Index(string token, string id)
        {
            using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
            {
                if (int.TryParse(id, out int value))
                {
                    // Kullanıcıdan URL'ye girilmesi istenilen İhalenin id bilgisi ve token değişkenleri oluşturulur.
                    id    = Request.QueryString["id"];
                    token = Request.QueryString["token"];

                    //Girilen token ve id veritabanındaki kayıt ile uyuşmuyorsa uygulamaya giriş yapmaması sağlanır.
                    if (token == null && id == null)
                    {
                        return View("Index");
                    }

                    // Eğer token ve id bilgisi doğru girilmişse giriş yapılan kullanıcının bilgileri veritabanından alınır.
                    else if (token != null && id != "" && id != null)
                    {
                        int intId = Convert.ToInt32(id);

                        var isTokenValid = db.Supplier.FirstOrDefault(x => x.Token == token);
                        var isIdValid    = db.Ihale.FirstOrDefault(y => y.IhaleId == intId);

                        //Giriş yapan kullanıcının token bilgisi varsa İhalenin id bilgisi, token ve kullanıcının id bilgisi Sessionda tutulur.
                        if (isTokenValid != null && isIdValid != null)
                        {
                            Session["id"]          = id;
                            Session["token"]       = token;
                            Session["supplier_id"] = isTokenValid.SupplierId;
                            return View("Index");
                        }
                        else
                        {
                            TempData["message"] = "Geçersiz giriş yapıldı!";
                            return View("Index");
                        }
                    }
                    else
                    {
                        TempData["message"] = "Geçersiz giriş yapıldı!";
                        return View("Index");
                    }
                }
            }
            return View("Index");
        }
        #endregion

        #region[Tedarikçilerin Profil Sayfası]
        [HttpGet]
        public ActionResult Profile()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                string token    = (string)Session["token"];
                string username = (string)Session["username"];

                using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                {
                    int supplierId = 1;
                    List<Supplier> profile = new List<Supplier>();

                    if (Session["token"] != null)
                    {
                        profile    = db.Supplier.Where(x => x.Token == token).ToList();
                        supplierId = profile[0].SupplierId;
                    }

                    if (Session["username"] != null)
                    {
                        profile    = db.Supplier.Where(y => y.Username == username).ToList();
                        supplierId = profile[0].SupplierId;
                    }

                    var supplierOffers  = db.Offer.Include("Ihale").Where(x => x.SupplierId == supplierId).Where(u => u.IsActiveOffer == true).ToList();
                    var notActiveOffers = db.Offer.Include("Ihale").Where(x => x.SupplierId == supplierId).Where(o => o.IsActiveOffer == false).ToList();

                    //Tedarikçilerin bir ihaleye yaptığı tekliflerin sıralanarak getirilmesi için yazılan linq.
                    var sequenceSupplier       = db.Offer.Include("Ihale").Where(x => x.SupplierId == supplierId).Where(q => q.IsActiveOffer == true).Select(i => new { i.OfferId, i.IhaleId, i.OfferPrice, i.OfferDailyCurrency }).ToArray();
                    Dictionary<int, int> datam = new Dictionary<int, int>();
                    for (int i                 = 0; i < sequenceSupplier.Length; i++)
                    {
                        //Order değişkeni teklif büyüklüğüne göre sıra bilgisini tutmak içindir.
                        var order             = 1;
                        //Tedarikçinin yaptığı teklif değeri. TL,Dolar veya Euro cinsinden.
                        var a                 = sequenceSupplier[i].OfferPrice;
                        //Tedarikçinin teklif yaptığı anda para biriminin kur değeri.
                        var b                 = sequenceSupplier[i].OfferDailyCurrency;
                        //Teklif değeri ve para biriminin kur değeri çarpılarak bu çarpım değerleri aynı birimde(TL cinsinden)sıralaması yapılır.
                        var supplierTeklif    = a * b;
                        //Bir ihaleye yapılan diğer tekliflerin bulunması için.
                        var arrayIndexIhaleId = sequenceSupplier[i].IhaleId;
                        var arrayIndexOfferId = sequenceSupplier[i].OfferId;
                        //Diğer tekliflerin getirildiği linq sorgusu.
                        var otherOffers       = db.Offer.Where(x => x.IhaleId == arrayIndexIhaleId).Where(p => p.OfferId != arrayIndexOfferId).Where(q => q.IsActiveOffer == true).Select(z => new { z.OfferPrice, z.OfferDailyCurrency }).ToArray();
                        for (int k            = 0; k < otherOffers.Length; k++)
                        {
                            //Diğer teklifin değeri.
                            var digerTeklifPrice    = otherOffers[k].OfferPrice;
                            //Diğer teklifin yapıldığı anda para biriminin kur değeri.
                            var digerTeklifCurrency = otherOffers[k].OfferDailyCurrency;
                            //İki değerin çarpılması sonucunda aynı birimde(TL cinsinden)sıralaması yapılır.
                            var digerTeklif         = digerTeklifPrice * digerTeklifCurrency;

                            if (supplierTeklif > digerTeklif)
                            {
                                //Sıra bilgisi karşılaştırma yapılarak güncellenir.
                                order++;
                            }
                        }
                        datam.Add(sequenceSupplier[i].OfferId, order);
                    }

                    ViewData["siralama"]        = datam;
                    ViewData["supplierOffers"]  = supplierOffers;
                    ViewData["notActiveOffers"] = notActiveOffers;
                    ViewData["profile"]         = profile;

                    return View();
                }
            }
            return View("Index");
        }
        #endregion

        #region[Admin Profil Sayfası- AdminProfile]
        [HttpGet]
        public ActionResult AdminProfile()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                {
                    //Aktif olan ihalelerin veritabanından çekildiği linq sorgusu
                    var tumIhaleler         = db.Ihale.Include("Supplier").OrderByDescending(i => i.IsActive == true).ToList();
                    ViewData["tumIhaleler"] = tumIhaleler;
                    return View("Profile");
                }
            }
            return View("Index");
        }
        #endregion

        #region[İhalelerin Filtrelenmesi]
        /// <summary>
        /// İhale görüntülemede filtreleme yaparak kolaylık sağlanması
        /// </summary>
        /// <param name="ihaleCondition"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult FilterIhale(string ihaleCondition, string filter)
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && Session["isAdmin"] != null)
            {
                // Admin tüm ihalelere yaptığı filtreler -----------------------------------------------------
                if (ihaleCondition == "all" && filter == "none")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").OrderByDescending(i => i.IsActive == true).ToList();
                        TempData["filtre"]      = "Tüm İhaleler";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Tüm ihaleleri Şirket ismine göre A'dan Z'ye sıralamak.
                if (ihaleCondition == "all" && filter == "aToZ")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").OrderBy(i => i.IhaleName).ToList();
                        TempData["filtre"]      = "Tüm İhaleler - A'dan Z'ye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Tüm ihaleleri Şirket ismine göre Z'dan A'ya sıralamak.
                if (ihaleCondition == "all" && filter == "zToA")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").OrderByDescending(i => i.IhaleName).ToList();
                        TempData["filtre"]      = "Tüm İhaleler - Z'den A'ya";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Tüm ihaleleri başladığı zamana göre eskiden yeniye sıralamak.
                if (ihaleCondition == "all" && filter == "oldToNew")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").OrderBy(i => i.IhaleId).ToList();
                        TempData["filtre"]      = "Tüm İhaleler - Eskiden Yeniye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Tüm ihaleleri yeniden eskiye sıralamak.
                if (ihaleCondition == "all" && filter == "newToOld")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").OrderByDescending(i => i.IhaleId).ToList();
                        TempData["filtre"]      = "Tüm İhaleler - Yeniden Eskiye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                // Admin aktif ihalelere yaptığı filtreler -------------------------------------------------------
                if (ihaleCondition == "active" && filter == "none")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == true).ToList();
                        TempData["filtre"]      = "Aktif İhaleler";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Aktif ihalelerde Şirket ismine göre A'dan Z'ye sıralamak.
                if (ihaleCondition == "active" && filter == "aToZ")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == true).OrderBy(b => b.IhaleName).ToList();
                        TempData["filtre"]      = "Aktif İhaleler - A'dan Z'ye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Aktif ihalelerde Şirket ismine göre Z'den A'ya sıralamak.
                if (ihaleCondition == "active" && filter == "zToA")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == true).OrderByDescending(b => b.IhaleName).ToList();
                        TempData["filtre"]      = "Aktif İhaleler - Z'den A'ya ";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Aktif ihalelerde ihalenin başladığı zamanı eskiden yeniye sıralamak.
                if (ihaleCondition == "active" && filter == "oldToNew")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == true).OrderBy(b => b.IhaleId).ToList();
                        TempData["filtre"]      = "Aktif İhaleler - Eskiden Yeniye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //Aktif ihalelerde ihalenin başladığı zamana göre yeniden eskiye sıralamak.
                if (ihaleCondition == "active" && filter == "newToOld")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == true).OrderByDescending(b => b.IhaleId).ToList();
                        TempData["filtre"]      = "Aktif İhaleler - Yeniden Eskiye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                // Admin inaktif ihalelere yaptığı filtreler -----------------------------------------------------
                if (ihaleCondition == "inactive" && filter == "none")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == false).ToList();
                        TempData["filtre"]      = "İnaktif İhaleler";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //İnaktif ihalelerde Şirket ismine göre A'dan Z'ye sıralamak.
                if (ihaleCondition == "inactive" && filter == "aToZ")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == false).OrderBy(b => b.IhaleName).ToList();
                        TempData["filtre"]      = "İnaktif İhaleler - A'dan Z'ye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //İnaktif ihalelerde Şirket ismine göre Z'dan A'ye sıralamak. 
                if (ihaleCondition == "inactive" && filter == "zToA")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == false).OrderByDescending(b => b.IhaleName).ToList();
                        TempData["filtre"]      = "İnaktif İhaleler - Z'den A'ya";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //İnaktif ihalelerde ihalenin başladığı zamana göre eskiden yeniye sıralamak.
                if (ihaleCondition == "inactive" && filter == "oldToNew")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == false).OrderBy(b => b.IhaleId).ToList();
                        TempData["filtre"]      = "İnaktif İhaleler - Eskiden Yeniye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }

                //İnaktif ihalelerde ihalenin başladığı zamana göre yeniden eskiye sıralamak.
                if (ihaleCondition == "inactive" && filter == "newToOld")
                {
                    using (IhaleProject.Models.ihaleEntities db = new ihaleEntities())
                    {
                        var tumIhaleler         = db.Ihale.Include("Supplier").Where(a => a.IsActive == false).OrderByDescending(b => b.IhaleId).ToList();
                        TempData["filtre"]      = "İnaktif İhaleler - Yeniden Eskiye";
                        ViewData["tumIhaleler"] = tumIhaleler;
                        return View("Profile");
                    }
                }
            }
            return View("Index");

        }
        #endregion

        #region[Admin ve Tedarikçilerin Şifre İle Giriş Yaptığı Sayfa]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Login()
        {
            //Sessionda tutulan İhale'nin id bilgisi.
            string id = (string)Session["id"];

            //Kullanıcının giriş yapabilmesi için girmesi gereken kullanıcı adı ve parola.
            string password = Request.Form["Password"];
            string username = Request.Form["Username"];

            //Sessionda tutulan token bilgisi.
            string token = (string)Session["token"];

            //Byte tipine çevrilmiş iki şifre bilgisinin karşılaştırılması
            bool ByteArraysEqual(byte[] buffer3, byte[] buffer4)
            {
                // Kullanıcının girdiği iki şifre de birbiriyle eşleşiyorsa kullanıcının girdiği şifre doğrudur.
                if (buffer3 == buffer4) return true;

                // İstenilen 
                if (buffer3 == null || buffer4 == null) return false;

                // Girilen şifreler birbine eşit değilse false döndürülür.
                if (buffer3.Length != buffer4.Length) return false;
                for (int i = 0; i < buffer3.Length; i++)
                {
                    if (buffer3[i] != buffer4[i]) return false;
                }
                return true;
            }

            // İki tane hashlenmiş şifrenin karşılaştırılmasını sağlayan fonksiyon.
            bool VerifyHashedPassword(string hashedPassword, string passwordCompare)
            {
                byte[] buffer4;

                //Hashlenmiş şifre null geliyorsa false döndürülür.
                if (hashedPassword == null)
                {
                    return false;
                }

                // Karşılaştırılacak hashlenmiş şifre null geliyorsa hata döndürülür.
                if (passwordCompare == null)
                {
                    throw new ArgumentNullException("password");
                }
                byte[] src = Convert.FromBase64String(hashedPassword);

                if ((src.Length != 0x31) || (src[0] != 0))
                {
                    return false;
                }
                byte[] dst                      = new byte[0x10];
                Buffer.BlockCopy(src, 1, dst, 0, 0x10);
                byte[] buffer3                  = new byte[0x20];
                Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
                using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(passwordCompare, dst, 0x3e8))
                {
                    buffer4 = bytes.GetBytes(0x20);
                }
                return ByteArraysEqual(buffer3, buffer4);
            }

            using (ihaleEntities db = new ihaleEntities())
            {
                List<Supplier> user   = new List<Supplier>();
                bool isLoginWithToken = false;
                //Kullanıcılardan bir tanesini çekmek için linq.
                if (token != null)
                {
                    user             = db.Supplier.Where(x => x.Token == token).ToList();
                    isLoginWithToken = true;
                }

                if (username != null)
                {
                    user             = db.Supplier.Where(y => y.Username == username).ToList();
                    isLoginWithToken = false;
                }
                if (user.Count == 0)
                {
                    TempData["message"] = "Yanlış bilgi girildi.";
                    return RedirectToAction("Index");
                }
                //Kullanıcının şifresi hashlenmiş şifreye eşitlenir.
                var hashedPassword = user[0].Password;

                //Fonksiyon yardımıyla şifreler karşılaştırılır. Şifreler eşleşiyorsa kullanıcı giriş yapar.
                bool karsilastirma = VerifyHashedPassword(hashedPassword, password);

                if (karsilastirma == true)
                {
                    if (isLoginWithToken == false)
                    {
                        if (user[0].IsAdmin == true)
                        {
                            //Kullanıcı adminse Sessionda bu bilgi tutulur.
                            Session["isAdmin"]  = true;
                            Session["username"] = username;
                            Session["pwd"]      = 1;
                            return RedirectToAction("AdminProfile");
                        }

                        else
                        {
                            Session["pwd"]         = 1;
                            Session["supplier_id"] = user[0].SupplierId;
                            Session["username"]    = username;
                            return RedirectToAction("Profile");
                        }
                    }

                    //Kullanıcının admin mi tedarikçi mi olduğu kontrol edilir.
                    if (user[0].IsAdmin == true)
                    {
                        using (IhaleProject.Models.ihaleEntities db1 = new ihaleEntities())
                        {
                            //Kullanıcı adminse Sessionda bu bilgi tutulur.
                            Session["isAdmin"] = true;
                            Session["pwd"]     = 1;
                            return RedirectToAction("AdminProfile");
                        }
                    }

                    else
                    {
                        using (ihaleEntities ihale = new ihaleEntities())
                        {
                            //Kullanıcı admin değilse tedarikçidir. Tedarikçinin teklif yapacağı ihalenin bulunması gerekir.
                            int intId = Convert.ToInt32(id);

                            //Tedarikçinin hangi ihaleye teklif yapacağı bulunur.
                            var kontrolEdilecekIhale = db.Ihale.FirstOrDefault(x => x.IhaleId == intId);

                            //İhalenin bitiş tarihi veritabanından çekilir.
                            var kontrolEdilecekIhaleSaat = kontrolEdilecekIhale.IhaleLastTime;

                            DateTime currentDate = DateTime.Now;

                            // Eğer ihale saati geçmiş ise ihale aktiflikten çıkartılır ve kullanıcı profile yönlendirilir.
                            if (kontrolEdilecekIhaleSaat < currentDate)
                            {
                                var silinecekTeklifler = db.Offer.Where(x => x.IhaleId == intId).ToList();
                                //Hem silinecek teklif hem de ihale varsa
                                if (silinecekTeklifler != null && kontrolEdilecekIhale != null)
                                {
                                    foreach (var item in silinecekTeklifler)
                                    {
                                        item.IsActiveOffer = false;
                                    }
                                    Session["pwd"]                = 1;
                                    kontrolEdilecekIhale.IsActive = false;
                                    db.SaveChanges();
                                    return RedirectToAction("Profile");
                                }
                            }

                            //Tedarikçinin teklif yapabileceği bir ihale olması için ihalenin kazanılmamış olması ve aktif olması gerekir.
                            var teklifYapilacakIhale = ihale.Ihale.Where(p => p.WinnerSupplierId == null).Where(y => y.IsActive == true).FirstOrDefault(x => x.IhaleId == intId);


                            if (teklifYapilacakIhale != null)
                            {
                                //Teklif yapılabilecek bir ihale varsa ihaleye yapılacak teklifin bilgileri tutularak kaydedilir.
                                Session["pwd"]              = 1;
                                Session["isActive"]         = true;
                                Session["WinnerSupplierId"] = null;
                                ViewData["IhaleName"]       = teklifYapilacakIhale.IhaleName;
                                ViewData["IhaleLastTime"]   = teklifYapilacakIhale.IhaleLastTime;
                                ViewData["IhaleAdet"]       = teklifYapilacakIhale.IhaleAdet;
                                ViewData["IhaleAdetCins"]   = teklifYapilacakIhale.IhaleAdetCins;
                                //Tedarikçinin teklif yaptığı ihalenin son gününden günümüz çıkarılarak ihalenin bitimine kalan zaman hesaplanır.
                                var saatFark                = teklifYapilacakIhale.IhaleLastTime - DateTime.Now;
                                ViewData["saatFark"]        = saatFark;
                                return View("Offer");
                            }

                            //Kullanıcı token ile girer, linkteki ihale aktif değildir
                            else
                            {
                                Session["pwd"] = 1;
                                return RedirectToAction("Profile");
                            }
                        }
                    }
                }
                TempData["message"] = "Şifre yanlış girildi.";
                return View("Index");
            }
        }
        #endregion

        #region[İhale Aktiflik Kontrolü]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult IhaleSil()
        {
            string id = Request.Form["ihaleId"];
            int intId = Convert.ToInt32(id);

            if (intId != null && intId > 0 && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Bir ihalenin aktiflik durumu değiştirilecekse hangi ihale olduğunu bulabilmek için yazılan linq sorgusu.
                    var silinecekIhale     = db.Ihale.Where(x => x.IhaleId == intId).FirstOrDefault();
                    //İhale aktiflik durumu değiştirileceğinden o ihaleye yapılmış tekliflerin de durumunun değiştirilmesi gerekir.
                    var silinecekTeklifler = db.Offer.Where(x => x.IhaleId == intId).ToList();

                    var aktifTeklifler = db.Offer.Where(x => x.IhaleId == intId).Where(y => y.IsActiveOffer == true).ToList();
                    //Hem silinecek teklif hem de ihale varsa
                    if (silinecekTeklifler != null && silinecekIhale != null)
                    {
                        foreach (var item in silinecekTeklifler)
                        {
                            //İhale aktifse teklif pasif duruma geçirilir.
                            if (item.Ihale.IsActive == true)
                            {
                                item.IsActiveOffer = false;
                            }
                            //İhale aktif değilse teklif aktif duruma geçirilir.
                            //if (item.Ihale.IsActive == false)
                            //{
                            //    aktifTeklifler[0].IsActiveOffer = true;                              
                            //}
                        }
                        //İşlemler sonucunda ihalenin aktiflik durumunun tersi alınır. İhalenin aktiflik durumu değişmiş olur.
                        silinecekIhale.IsActive = !silinecekIhale.IsActive;
                        db.SaveChanges();
                        return RedirectToAction("AdminProfile");
                    }
                    return Content("Yanlış silme işlemi");
                }
            }
            return Content("yanlis islem");
        }
        #endregion

        #region[İhale Bilgilerini Düzenleme View Ekranı]       
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult IhaleDuzenle()
        {
            string id = Request.Form["ihaleId"];
            int intId = Convert.ToInt32(id);

            if (intId != null && intId > 0 && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Editlenecek ihaleyi bulduran linq sorgusu.
                    var editlenecekIhale = db.Ihale.Where(x => x.IhaleId == intId).ToList();
                    ViewData["editlenecekIhale"] = editlenecekIhale;
                    //Editlenecek ihalede seçilmiş ihalenin adet cins bilgisi(metre,kg vs.)kullanılmak için getirilir.
                    ViewBag.cins = editlenecekIhale[0].IhaleAdetCins;
                    return View();
                }
            }
            return Content("bir hata meydana geldi");
        }
        #endregion

        #region[İhale Bilgilerini Düzenleme Veritabanı]
        /// <summary>
        /// İhale bilgilerinin düzenlenip veritabanına kaydedildiği sayfa.
        /// </summary>
        /// <param name="IhaleId"></param>
        /// <param name="ihale"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult DuzenleIhale(int IhaleId, Ihale ihale)
        {
            using (ihaleEntities db = new ihaleEntities())
            {
                var data = db.Ihale.FirstOrDefault(x => x.IhaleId == IhaleId);
                var isActive = Request.Form["IsActive"];

                if(isActive == "on")
                {
                    data.IsActive = true;
                }

                if(isActive == "off")
                {
                    data.IsActive = false;
                }
                string id = Request.Form["IhaleId"];
                int intId = Convert.ToInt32(id);
               
                
                if (data != null && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && Session["isAdmin"] != null)
                {
                    //Veritabanında önceden kaydedilmiş ve editlenecek ihalenin bilgileri güncellenirse veritabanında güncellemenin yapıldığı yer.
                    
                    data.IhaleName = ihale.IhaleName;
                    data.IhaleAdet = ihale.IhaleAdet;
                    data.IhaleAdetCins = ihale.IhaleAdetCins;
                    data.IhaleLastTime = ihale.IhaleLastTime;                   
                    db.SaveChanges();
                    string token = (string)Session["token"];
                    string usernameSession = (string)Session["username"];

                    return RedirectToAction("AdminProfile");
                }
                else
                    return Content("Veri düzenlenemedi");
            }
        }
        #endregion

        #region[Tedarikçilerin Yaptığı Tekliflerin Sıralanarak Gösterilmesi]
        [HttpGet]
        public ActionResult IhaleDetaylar()
        {
            var id = (string)RouteData.Values["id"];
            int intId = Convert.ToInt32(id);

            if (intId > 0 && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Adminin tedarikçilerin yaptığı teklifleri sıralanmış bir halde görmesini sağlayan yer. 
                    var ihale = db.Ihale.Include("Supplier").Where(x => x.IhaleId == intId).ToList();
                    //İhaleye yapılan teklif değerleri büyükten küçüğe sıralanır.
                    //var a                  = db.Offer.ToList().OrderByDescending(e => Convert.ToDouble(e.OfferPrice)).ToList();
                    var teklifler = db.Offer.Where(x => x.IhaleId == intId).Where(q => q.IsActiveOffer == true).ToList().OrderBy(r => Convert.ToDouble(r.OfferPrice) * Convert.ToDouble(r.OfferDailyCurrency)).ToList();
                    var person = db.Offer.Include("Supplier").ToList();
                    ViewData["ihale"] = ihale;
                    ViewData["teklifler"] = teklifler;
                    ViewData["teklifSayisi"] = teklifler.Count;
                    return View();
                }
            }
            return Content("Hatalı girişim!");
        }
        #endregion

        #region[Hata Döndürme]
        public ActionResult PageNotFound()
        {
            return View("Error");
        }
        #endregion

        #region[Admin ve Tedarikçilerin Sessionda Tutulan Tüm Bilgilerinin Silinmesi]
        [HttpGet]
        public ActionResult Logout()
        {
            //Giriş yapmış kullanıcı Çıkış Yap butonuna tıkladığında Sessionda tutulan tüm bilgilerin silinmesi sağlanır.
            Session.Clear();
            return View("Index");
        }
        #endregion

        #region[Tedarikçilerin Teklif Yapacakları İhale'nin Sayfası]
        [HttpGet]
        public ActionResult Offer()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                string id = (string)Session["id"];

                using (ihaleEntities ihale = new ihaleEntities())
                {
                    int intId = Convert.ToInt32(id);
                    //Tedarikçinin teklif yapacağı ihaleyi bulduran linq sorgusu.
                    var teklifYapilacakIhale = ihale.Ihale.FirstOrDefault(x => x.IhaleId == intId);

                    //Teklif yapılacak ihale varsa bilgiler Viewdata ile tutulur.
                    if (teklifYapilacakIhale != null)
                    {
                        Session["pwd"] = 1;
                        ViewData["IhaleName"] = teklifYapilacakIhale.IhaleName;
                        ViewData["IhaleLastTime"] = teklifYapilacakIhale.IhaleLastTime;
                        ViewData["IhaleAdet"] = teklifYapilacakIhale.IhaleAdet;
                        ViewData["IhaleAdetCins"] = teklifYapilacakIhale.IhaleAdetCins;
                        //İhaleye yapılan teklif zamanı ile ihalenin bitiş günü arasındaki farkı bulma.
                        var saatFark = teklifYapilacakIhale.IhaleLastTime - DateTime.Now;
                        ViewData["saatFark"] = saatFark;
                        return View("Offer");
                    }
                }
            }
            return View("Index");
        }
        #endregion

        #region[Tedarikçinin Yaptığı Teklifin Veritabanında Tutulması-Offer]
        /// <summary>
        /// Teklife Dosya Ekleme.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Offer(HttpPostedFileBase file)
        {
            if (Request.Form["OfferPrice"] == "" && Request.Form["OfferDescription"] == "")
            {
                TempData["message"] = "Eksik bilgi gönderimi";
                return View("Profile");
            }
            Offer newRecord = new Offer();
            ihaleEntities db = new ihaleEntities();

            //Tedarikçi teklif yaparken dosya ekliyor mu kontrolünün yapılması.
            if (file != null)
            {
                string ImageName = System.IO.Path.GetFileName(file.FileName);
                string ext = System.IO.Path.GetExtension(file.FileName);
                long size = file.ContentLength;

                //Tedarikçi dosya eklemek isterse verilen tiplerde ekleme yapabilir.
                if ((ext.ToLower() == ".jpg" || ext.ToLower() == ".png" || ext.ToLower() == ".gif" || ext.ToLower() == ".jpeg" || ext.ToLower() == ".pdf" || ext.ToLower() == ".txt") && (size < 5000000))
                {
                    try
                    {
                        string physicalPath = Server.MapPath("~/Uploads/" + ImageName);
                        string physicalFolder = Server.MapPath("~/Uploads");

                        if (System.IO.Directory.Exists(physicalFolder))
                        {
                            // save image in folder
                            file.SaveAs(physicalPath);
                            newRecord.OfferImage = ImageName;
                        }
                        else
                        {
                            Directory.CreateDirectory(physicalFolder);
                            file.SaveAs(physicalPath);
                            newRecord.OfferImage = ImageName;
                        }

                    }
                    catch (Exception e)
                    {
                        ViewBag.hata = e.Message + " " + e.StackTrace;
                        TempData["message"] = e.Message + " " + e.StackTrace;
                        return View("Offer");
                    }

                }
                else
                {
                    TempData["message"] = "Dosya istenilen formatta değil";
                    return View("Offer");
                }
            }

            //Yapılan teklifteki değer.
            var doubleOfferPrice = (Request.Form["OfferPrice"]);

            //Teklif double bir teklifse veritabanına ondalıklı şekilde kaydetmek için replace yapılır.
            doubleOfferPrice = doubleOfferPrice.Replace('.', ',');
            newRecord.OfferPrice = Convert.ToDouble(doubleOfferPrice);
            var doubleOfferUnitPrice = Convert.ToDouble(Request.Form["OfferUnitPrice"]);

            //Günlük kur bilgisi TCMB'den çekilir.
            //XDocument Doc = XDocument.Load("https://www.tcmb.gov.tr/kurlar/today.xml");
            //var Dox = Doc.Descendants()
            //    .Where(r => r.Name == "Currency")
            //    .Select(r => new
            //    {
            //        //Günlük kur bilgisinin alış kur bilgisi çekilir.
            //        AlisKur = r.Element("ForexBuying").Value,
            //    });

            //Eğer tedarikçi yaptığı teklifi Dolar cinsinden yaparsa o günün dolar verisi TCMB'den çekilir.
            //if (Request.Form["OfferCurrency"] == "USD")
            //{
            //    //Dolar verisinin paylaşıldığı listedeki indexi [0] olduğundan bu bilgi alınır.
            //    //var usdData = Dox.ToArray()[0].AlisKur;
            //    //usdData = usdData.Replace('.', ',');
            //    //var decimalUsdData = Double.Parse(usdData, System.Globalization.NumberStyles.Currency);
            //    //newRecord.OfferDailyCurrency = decimalUsdData;
            //}

            //Eğer tedarikçi yaptığı teklifi Euro cinsinden yaparsa o günün euro verisi TCMB'den çekilir.
            //else if (Request.Form["OfferCurrency"] == "EUR")
            //{
            //    //Dolar verisinin paylaşıldığı listedeki indexi[3] olduğundan bu bilgi alınır.
            //    var eurData = Dox.ToArray()[3].AlisKur;
            //    eurData = eurData.Replace('.', ',');
            //    var decimaleurData = Double.Parse(eurData, System.Globalization.NumberStyles.Currency);
            //    newRecord.OfferDailyCurrency = decimaleurData;
            //}

            //Eğer tedarikçi teklifini TL cinsinden yaparsa sıralama TL cinsinden yapılacağından TL verisinin TCMB'den çekilmesine gerek yoktur.
            if (Request.Form["OfferCurrency"] == "TL" || Request.Form["OfferCurrency"] == "USD" || Request.Form["OfferCurrency"] == "EUR")
            {
                newRecord.OfferDailyCurrency = Convert.ToDouble("1");
            }

            DateTime CurrentDate2 = DateTime.Now;
            newRecord.OfferTime = CurrentDate2;
            newRecord.OfferCurrency = Request.Form["OfferCurrency"];
            newRecord.OfferDescription = Request.Form["OfferDescription"];
            newRecord.SupplierId = (int)Session["supplier_id"];
            string id = (string)Session["id"];
            int intId = Convert.ToInt32(id);
            var kontrolIhaleAktifligi = db.Ihale.Where(x => x.IhaleId == intId).ToList();

            //İhale aktifse yapılan teklif de aktif duruma geçirilir.
            if (kontrolIhaleAktifligi[0].IsActive == true)
            {
                newRecord.IsActiveOffer = true;
            }
            else
            {
                newRecord.IsActiveOffer = false;
            }
            var intIhaleId = Convert.ToInt32(Session["id"]);
            newRecord.IhaleId = intIhaleId;

            var supplierId = (int)Session["supplier_id"];
            var iptalEdilecekTeklifler = db.Offer.Where(a => a.IhaleId == intIhaleId).Where(s => s.SupplierId == supplierId).ToList();

            if (iptalEdilecekTeklifler.Count > 0)
            {
                foreach (var item in iptalEdilecekTeklifler)
                {
                    item.IsActiveOffer = false;
                }
            }

            //Yapılan teklif veritabanına kaydedilir.
            db.Offer.Add(newRecord);
            db.SaveChanges();
            return RedirectToAction("Profile");
        }
        #endregion

        #region[İhalede Kazanan Teklifin Belirlenmesi]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult AcceptOffer()
        {
            string id = Request.Form["ihaleId"];
            int intId = Convert.ToInt32(id);

            if (intId != null && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                var ihaleId = Request.Form["IhaleId"];
                var supplierId = Request.Form["SupplierId"];
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Yapılan teklif sonucunda adminin seçtiği teklifin bulunduğu ihaleyi buldurmak için linq sorgusu.
                    var kabulEdilenIhale = db.Ihale.Where(x => x.IhaleId == intId).FirstOrDefault();
                    //Kazanan teklifin buldurulacağı linq sorgusu.
                    var kabulEdilenTeklif = db.Offer.Where(x => x.IhaleId == intId).ToList();

                    if (kabulEdilenIhale != null && kabulEdilenTeklif != null)
                    {
                        foreach (var item in kabulEdilenTeklif)
                        {
                            //Teklif kabul edilmişse teklifin aktiflik durumu pasife çekilir.
                            item.IsActiveOffer = false;
                        }
                        //İhalede kazanan bir teklif bulunduğundan ihalenin aktiflik durumu pasife çekilir.
                        kabulEdilenIhale.IsActive = false;
                    }
                    int intIhaleId = Convert.ToInt32(ihaleId);
                    int intSupplierId = Convert.ToInt32(supplierId);

                    if (intIhaleId > 0 && intSupplierId > 0)
                    {
                        var ihale = db.Ihale.First(x => x.IhaleId == intIhaleId);
                        //Kazanan tedarikçinin tutulduğu değişken.
                        ihale.WinnerSupplierId = intSupplierId;
                        Session["WinnerSupplierId"] = intSupplierId;
                        db.SaveChanges();
                        return RedirectToAction("AdminProfile");
                    }
                }
            }
            return null;
        }
        #endregion

        #region[İhaleyi Kazanan Tedarikçinin Kaldırılması]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult ChangeWinner()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                var ihaleId = Request.Form["IhaleId"];
                int intIhaleId = Convert.ToInt32(ihaleId);

                if (intIhaleId > 0)
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Herhangi bir tedarikçi önceden bir ihaleyi kazanmışsa bunu değiştirmek sonucunda 
                        var ihale = db.Ihale.First(x => x.IhaleId == intIhaleId);
                        ihale.WinnerSupplierId = null;
                        db.SaveChanges();
                        return RedirectToAction("AdminProfile");
                    }
                }
            }
            return null;
        }
        #endregion

        #region[İhale Oluşturma Sayfası View]
        [HttpGet]
        public ActionResult IhaleOlustur()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                return View();
            }
            return null;
        }
        #endregion

        #region[İhale Oluşturma Veritabanı Kaydı]
        /// <summary>
        /// İhale Oluşturma.
        /// </summary>
        /// <param name="ihale"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult IhaleOlustur(Ihale ihale, HttpPostedFileBase file)
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Eğer admin ihale oluştururken bir dosya eklemek isterse.
                    if (file != null)
                    {
                        string ImageName = System.IO.Path.GetFileName(file.FileName);
                        string ext = System.IO.Path.GetExtension(file.FileName);
                        long size = file.ContentLength;

                        //Dosya tipleri tanımlanmıştır.
                        if ((ext.ToLower() == ".jpg" || ext.ToLower() == ".png" || ext.ToLower() == ".gif" || ext.ToLower() == ".jpeg" || ext.ToLower() == ".pdf" || ext.ToLower() == ".txt") && (size < 5000000))
                        {
                            string phtsicalFolder = Server.MapPath("~/Uploads");
                            if(!Directory.Exists(phtsicalFolder))
                            {
                                Directory.CreateDirectory(phtsicalFolder);
                            }
                            string physicalPath = Path.Combine(phtsicalFolder, ImageName);
                            //Dosya kaydedilir.
                            file.SaveAs(physicalPath);
                            ihale.IhaleImage = ImageName;
                        }
                        else
                        {
                            //Dosya belirtilen formatlardan biri değilse mesaj bastırılır.
                            TempData["message"] = "Dosya istenilen formatta değil";
                            return RedirectToAction("Profile");
                        }
                    }
                    //Admin ihaleyi oluşturduğunda oluşan ihalenin aktiflik durumu kontrol edilir.
                    ihale.IsActive = true;
                    db.Ihale.Add(ihale);
                    db.SaveChanges();
                    return RedirectToAction("AdminProfile");
                }
            }
            return null;
        }
        #endregion

        #region[İhalenin Bitiş Gününe Ne Kadar Kaldığını Geri Sayarak Gösterme]
        [HttpGet]
        public ActionResult GetClockData()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //İhalenin id'si tanımlanır.
                    string id = (string)Session["id"];
                    int intId = Convert.ToInt32(id);
                    //Tedarikçinin hangi ihaleye teklif yapacağı bulunur.
                    var teklifYapilacakIhale = db.Ihale.FirstOrDefault(x => x.IhaleId == intId);
                    //İhalenin bitiş tarihi veritabanından çekilir.
                    var teklifYapilacakIhaleSaat = teklifYapilacakIhale.IhaleLastTime;
                    DateTime currentDate = DateTime.Now;
                    //İhalenin bitiş günü ile tedarikçinin teklif yaptığı gün arasındaki değişken.
                    var saatFarki = teklifYapilacakIhaleSaat - currentDate;
                    return Json(saatFarki, JsonRequestBehavior.AllowGet);

                }
            }
            return null;
        }
        #endregion

        #region[Teklifin Aktif Halden Pasif Hale Çekilmesi]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult DeleteOffer()
        {
            var OfferId = Request.Form["OfferId"];
            int intOfferId = Convert.ToInt32(OfferId);

            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && intOfferId > 0)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Silinmek istenen teklif linq sorgusu ile bulunur.
                    var silinecekTeklif = db.Offer.Where(x => x.OfferId == intOfferId).ToList();

                    //Silinecek teklifin aktiflik durumu pasife çekilir.
                    if (silinecekTeklif[0].IsActiveOffer == true)
                    {
                        silinecekTeklif[0].IsActiveOffer = false;
                        db.SaveChanges();
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        return RedirectToAction("Profile");
                    }
                }
            }
            return null;
        }
        #endregion

        #region[Teklifin Pasif Halden Aktif Hale Çekilmesi]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult ReturnOffer()
        {
            //Aktiflik durumu pasif olan teklifi aktif hale geçirmek için yazılan fonksiyon
            var OfferId = Request.Form["OfferId"];
            int intOfferId = Convert.ToInt32(OfferId);

            var IhaleId = Request.Form["IhaleId"];
            int intIhaleId = Convert.ToInt32(IhaleId);

            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && intOfferId > 0 && intIhaleId > 0)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Aktifliği değiştirilecek teklif linq sorgusu ile bulunur.
                    var geriDondurulecekTeklif = db.Offer.Where(x => x.OfferId == intOfferId).ToList();

                    var supplierId = (int)Session["supplier_id"];
                    var iptalEdilecekTeklifler = db.Offer.Where(a => a.IhaleId == intIhaleId).Where(s => s.SupplierId == supplierId && s.OfferId != intOfferId).ToList();

                    if (iptalEdilecekTeklifler.Count > 0)
                    {
                        foreach (var item in iptalEdilecekTeklifler)
                        {
                            item.IsActiveOffer = false;

                        }
                        db.SaveChanges();
                    }

                    //Teklifin aktifliği pasif durumdaysa aktif duruma geçirilir.
                    if (geriDondurulecekTeklif[0].IsActiveOffer == false)
                    {
                        geriDondurulecekTeklif[0].IsActiveOffer = true;
                        db.SaveChanges();
                        return RedirectToAction("Profile");
                    }
                    else
                    {
                        return RedirectToAction("Profile");
                    }
                }
            }
            return null;
        }
        #endregion

        #region[Tedarikçi Oluşturma View]
        [HttpGet]
        public ActionResult TedarikciOlustur()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                return View();
            }
            return null;
        }
        #endregion

        #region[Tedarikçi Oluşturma Veritabanı Kaydı]
        /// <summary>
        /// Tedarikçi Oluşturma
        /// </summary>
        /// <param name="supplier"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult TedarikciOlustur(Supplier supplier)
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && Session["isAdmin"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    // Rastgele bir token oluşturma fonksiyonu
                    string RandomString(int length)
                    {
                        //Kullanıcılar için token oluşturma.
                        const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
                        StringBuilder res = new StringBuilder();
                        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                        {
                            byte[] uintBuffer = new byte[sizeof(uint)];
                            while (length-- > 0)
                            {
                                rng.GetBytes(uintBuffer);
                                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                                res.Append(valid[(int)(num % (uint)valid.Length)]);
                            }
                        }
                        return res.ToString();
                    }

                    //Kullanıcının girdiği şifreyi hashleme fonksiyonu
                    string HashPassword(string password)
                    {
                        byte[] salt;
                        byte[] buffer2;

                        if (password == null)
                        {
                            throw new ArgumentNullException("password");
                        }
                        using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
                        {
                            salt = bytes.Salt;
                            buffer2 = bytes.GetBytes(0x20);
                        }
                        byte[] dst = new byte[0x31];
                        Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
                        Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
                        return Convert.ToBase64String(dst);
                    }

                    var kontrolEdilecekUsername = db.Supplier.Where(t => t.Username == supplier.Username).FirstOrDefault();
                    if (kontrolEdilecekUsername == null && supplier.Username.Length > 8)
                    {
                        //Kullanıcıyı admin oluşturacağından sadece tedarikçiler oluşturulur.Bu yüzden adminlik durumu false yapılır.
                        supplier.IsAdmin = false;
                        //Tedarikçi oluşturulduğundan supplier'ın durumu aktif yapılır.
                        supplier.IsSupplierActive = true;

                        //Token için oluşturulan fonksiyon veritabanında token'a eşitlenir.
                        supplier.Token = RandomString(64);

                        //Hashlenmiş şifre de veritabanına kaydedilir.
                        supplier.Password = HashPassword(supplier.Password);
                        db.Supplier.Add(supplier);
                        db.SaveChanges();
                        return RedirectToAction("AllSuppliers");
                    }
                    else
                    {
                        TempData["message"] = "Kullanıcı adı zaten mevcut";
                        return RedirectToAction("TedarikciOlustur");
                    }
                }
            }
            return Content("bir hata");
        }
        #endregion

        #region [Tedarikçi Filtreleme İşlemleri]
        /// <summary>
        /// 
        /// </summary>
        /// <param name="supplierCondition"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult FilterSuppliers(string supplierCondition, string filter)
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && Session["isAdmin"] != null)
            {
                // Admin aktif tedarikçileri seçtiği durumlar-------------------------------------------------------------------------
                if (supplierCondition == "active" && filter == "none")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == true).Where(y=>y.IsAdmin == false).ToList();
                        TempData["filtre"] = "Aktif Kullanıcılar";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "active" && filter == "aToZ")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == true).Where(y => y.IsAdmin == false).OrderBy(e => e.CompanyName).ToList();
                        TempData["filtre"] = "Aktif Kullanıcılar - A'dan Z'ye ";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "active" && filter == "zToA")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == true).Where(y => y.IsAdmin == false).OrderByDescending(w => w.CompanyName).ToList();
                        TempData["filtre"] = "Aktif Kullanıcılar - Z'den A'ya ";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "active" && filter == "oldToNew")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == true).Where(y => y.IsAdmin == false).OrderBy(w => w.SupplierId).ToList();
                        TempData["filtre"] = "Aktif Kullanıcılar - Eskiden Yeniye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "active" && filter == "newToOld")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == true).Where(y => y.IsAdmin == false).OrderByDescending(w => w.SupplierId).ToList();
                        TempData["filtre"] = "Aktif Kullanıcılar - Yeniden Eskiye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                // Adminin tüm tedarikçileri seçtiği durumlar---------------------------------------------------------------------------
                if (supplierCondition == "all" && filter == "none")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(y => y.IsAdmin == false).ToList();
                        TempData["filtre"] = "Tüm Kullanıcılar";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "all" && filter == "aToZ")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(y => y.IsAdmin == false).OrderBy(q => q.CompanyName).ToList();
                        TempData["filtre"] = "Tüm Kullanıcılar - A'dan Z'ye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "all" && filter == "zToA")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(y => y.IsAdmin == false).OrderByDescending(w => w.CompanyName).ToList();
                        TempData["filtre"] = "Tüm Kullanıcılar Z'den A'ya";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "all" && filter == "oldToNew")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(y => y.IsAdmin == false).OrderBy(w => w.SupplierId).ToList();
                        TempData["filtre"] = "Tüm Kullanıcılar - Eskiden Yeniye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "all" && filter == "newToOld")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(y => y.IsAdmin == false).OrderByDescending(w => w.SupplierId).ToList();
                        TempData["filtre"] = "Tüm Kullanıcılar - Yeniden Eskiye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                // Adminin inaktif tedarikçileri seçtiği durumlar -----------------------------------------------------------------------
                if (supplierCondition == "inactive" && filter == "none")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == false).Where(y => y.IsAdmin == false).ToList();
                        TempData["filtre"] = "Aktif olmayan Kullanıcılar";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "inactive" && filter == "aToZ")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == false).Where(y => y.IsAdmin == false).OrderBy(q => q.CompanyName).ToList();
                        TempData["filtre"] = "Aktif olmayan Kullanıcılar - A'dan Z'ye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "inactive" && filter == "zToA")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == false).Where(y => y.IsAdmin == false).OrderByDescending(q => q.CompanyName).ToList();
                        TempData["filtre"] = "Aktif olmayan Kullanıcılar - Z'den A'ya";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "inactive" && filter == "oldToNew")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == false).Where(y => y.IsAdmin == false).OrderBy(q => q.SupplierId).ToList();
                        TempData["filtre"] = "Aktif olmayan Kullanıcılar - Eskiden Yeniye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                if (supplierCondition == "inactive" && filter == "newToOld")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string token = (string)Session["token"];
                        var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(r => r.IsSupplierActive == false).Where(y => y.IsAdmin == false).OrderByDescending(q => q.SupplierId).ToList();
                        TempData["filtre"] = "Aktif olmayan Kullanıcılar - Yeniden Eskiye";
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

            }
            return Content("Lütfen tekrar giriş yapınız");
        }

        #endregion

        #region[Adminin Tüm Tedarikçileri Görmesi]
        [HttpGet]
        public ActionResult AllSuppliers(string supplierCondition, string filter)
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && Session["isAdmin"] != null)
            {
                if (supplierCondition == "all" && filter == "none")
                {
                    using (ihaleEntities db = new ihaleEntities())
                    {
                        //Adminin tüm kullanıcıları görebilmesi için oluşturulmuş linq sorgusu.
                        string tokenSession             = (string)Session["token"];
                        string usernameSession          = (string)Session["username"];
                        List<Supplier> gosterilecekVeri = new List<Supplier>();

                        if (tokenSession != null)
                        {
                            gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != tokenSession).Where(y=>y.IsAdmin == false).ToList();

                        }

                        if(usernameSession != null)
                        {
                            gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != usernameSession).Where(y => y.IsAdmin == false).ToList();
                        }
                        ViewData["gosterilecekVeri"] = gosterilecekVeri;
                        return View("AllSuppliers");
                    }
                }

                //string id = Request.Form["supplierId"];
                //int intId = Convert.ToInt32(id);

                string token = (string)Session["token"];
                using (ihaleEntities db = new ihaleEntities())
                {

                    var gosterilecekVeri = db.Supplier.Include("Offer").Where(x => x.Token != token).Where(y=>y.IsAdmin == false).ToList();
                    //Tedarikçilerin bilgileri düzenlenmek istenirse çekilen tedarikçiler linq.
                    //var editlenecekTedarikci = db.Supplier.Where(x => x.SupplierId == intId).ToList();
                    //ViewData["editlenecekTedarikci"] = editlenecekTedarikci;
                    ViewData["gosterilecekVeri"] = gosterilecekVeri;
                    return View();
                }
            }
            return Content("Something went wrong :(");
        }
        #endregion

        #region[Admin'in Tedarikçilerin Aktiflik Durumunu Değiştirmesi]
        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult ReturnTedarikci()
        {
            var SupplierId = Request.Form["SupplierId"];
            int intSupplierId = Convert.ToInt32(SupplierId);

            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null && intSupplierId > 0 && Session["isAdmin"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Tedarikçinin tüm tekliflerini pasif duruma geçirme
                    var silinecekTeklifler = db.Offer.Where(x => x.SupplierId == intSupplierId).ToList();
                    var geriDondurulecekTedarikci = db.Supplier.Where(x => x.SupplierId == intSupplierId).ToList();

                    if (silinecekTeklifler != null && geriDondurulecekTedarikci != null)
                    {
                        if (geriDondurulecekTedarikci[0].IsSupplierActive == true)
                        {
                            foreach (var item in silinecekTeklifler)
                            {
                                item.IsActiveOffer = false;
                            }
                        }
                        else
                        {
                            foreach (var item in silinecekTeklifler)
                            {
                                item.IsActiveOffer = true;
                            }
                        }
                        geriDondurulecekTedarikci[0].IsSupplierActive = !geriDondurulecekTedarikci[0].IsSupplierActive;
                        db.SaveChanges();
                        return RedirectToAction("AllSuppliers");
                    }
                    else
                    {
                        return RedirectToAction("AllSuppliers");
                    }
                }
            }
            return Content("Something went wrong :((");
        }
        #endregion

        #region[Tedarikçinin Aktiflik durumunun Değişmesi View]
        [HttpPost]
        public ActionResult DuzenleTedarikci()
        {
            string id = Request.Form["SupplierId"];
            int intId = Convert.ToInt32(id);

            if (intId != null && intId > 0 && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {
                using (ihaleEntities db = new ihaleEntities())
                {
                    //Editlenecek tedarikciyi bulduran linq sorgusu.
                    var editlenecekTedarikci = db.Supplier.Where(x => x.SupplierId == intId).ToList();
                    ViewData["editlenecekTedarikci"] = editlenecekTedarikci;

                    return View();
                }
            }
            return Content("bir hata var");
        }
        #endregion

        #region[Tedarikçinin Aktiflik durumunun Değişmesi Veritabanı]
        /// <summary>
        /// Tedarikçi Düzenleme.
        /// </summary>
        /// <param name="SupplierId"></param>
        /// <param name="supplier"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult TedarikciDuzenle(int SupplierId, Supplier supplier)
        {
            using (ihaleEntities db = new ihaleEntities())
            {
                string id = Request.Form["SupplierId"];
                int intId = Convert.ToInt32(id);
                var data = db.Supplier.FirstOrDefault(x => x.SupplierId == intId);

                if (data != null && (Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
                {
                    //Tedarikçilerin bilgileri güncellenmek istenirse veritabanı kaydını sağlar.
                    data.Address = supplier.Address;
                    data.CompanyName = supplier.CompanyName;
                    data.Email = supplier.Email;
                    data.Fax = supplier.Fax;
                    data.Phone = supplier.Phone;
                    db.SaveChanges();

                    var editlenecekSupplier = db.Supplier.Where(x => x.SupplierId == SupplierId).ToList();
                    return RedirectToAction("AllSuppliers");

                }
            }
            return Content("Hata");

        }
        #endregion

        #region[İletişim Get Fonksiyonu]
        [HttpGet]
        public ActionResult Contact()
        {
            if ((Session["token"] != null || Session["username"] != null) && Session["pwd"] != null)
            {

                return View();
            }
            return View("Error");
        }
        #endregion
    }
}
