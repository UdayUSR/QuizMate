using QuizMate.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace QuizMate.Controllers
{
    public class HomeController : Controller
    {
        QuizMateEntities db = new QuizMateEntities();
        

        public ActionResult Logout()
        {
            Session.Abandon();
            Session.RemoveAll();
            return RedirectToAction("Index");
            
        }
        [HttpGet]
        public ActionResult sregister()
        {

            return View();
        }
        [HttpPost]
        public ActionResult sregister(student svw, HttpPostedFileBase imgfile)
        {
            student s = new student();
            try
            {
                s.std_name = svw.std_name;
                s.std_pass = svw.std_pass;
                s.std_image = uploadimage(imgfile);
                db.student.Add(s);
                db.SaveChanges();
                return RedirectToAction("slogin");
            }
            catch (Exception)
            {
                ViewBag.msg = "Data Could Not Be Inserted";
                
            }
            
            return View();
        }
        public string uploadimage(HttpPostedFileBase imgfile)
        {
            string path = "-1";
            try
            {
                if(imgfile!=null && imgfile.ContentLength>0)
                {
                    string extension= Path.GetExtension(imgfile.FileName);
                    if(extension.ToLower().Equals("jpg")|| extension.ToLower().Equals("jpeg")|| extension.ToLower().Equals("png"))
                    {
                        Random r = new Random();
                        path=Path.Combine(Server.MapPath("~/Content/img"), r+Path.GetFileName(imgfile.FileName));
                        imgfile.SaveAs(path);
                        path = "~/Content/img" + r + Path.GetFileName(imgfile.FileName);
                    }
                }
            }
            catch (Exception)
            {

                
            }
            return path;
        }
        [HttpGet]
        public ActionResult tlogin()
        {
            
            return View();
        }
        [HttpPost]
        public ActionResult tlogin(tbl_admin a)
        {
            tbl_admin ad = db.tbl_admin.Where(x=>x.ad_name.Equals(a.ad_name) && x.ad_pass.Equals(a.ad_pass)).FirstOrDefault();
            if(ad!= null)
            {
                Session["ad_id"] = ad.ad_id;
                return RedirectToAction("Dashboard");
            }
            else
            {
                ViewBag.msg = "Invalid Username or Password";
            }
            return View();
        }
        public ActionResult slogin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult slogin(student s)
        {
            student std=db.student.Where(x=>x.std_name==s.std_name && x.std_pass==s.std_pass).FirstOrDefault();
            if(std==null)
            {
                ViewBag.msg = "Invalid Username or Password";
            }
            else
            {
                Session["stud_id"] = std.std_id;
                return RedirectToAction("StudentExam");
            }
            return View();
        }
        public ActionResult StudentExam()
        {
            if (Session["stud_id"] == null)
            {
                return RedirectToAction("slogin");
            }
            //else
            //{
            //    return RedirectToAction("StudentExam");
            //}
            return View();
        }
        [HttpPost]
        public ActionResult StudentExam(string room)
        {
            List<tbl_category> list = db.tbl_category.ToList();
            foreach (var item in list)
            {
                if(item.cat_encryptedstring==room)
                {
                    List<tbl_question> li = db.tbl_question.Where(x => x.q_fk_catid == item.cat_id).ToList();
                    Queue<tbl_question> queue=new Queue<tbl_question>();
                    foreach(tbl_question a in li)
                    {
                        queue.Enqueue(a);
                    }
                    //TempData["examid"]=item.cat_id;
                    TempData["questions"] = queue;
                    TempData["score"] = 0;
                    TempData.Keep();
                    return RedirectToAction("QuizStart");
                }
                else
                {
                    ViewBag.error = "No Room Found";
                }
            }
            return View();
        }
        
        public ActionResult QuizStart()
        {
            if (Session["stud_id"] == null)
            {
                return RedirectToAction("slogin");
            }
            tbl_question q = null;
            if(TempData["questions"]!=null)
            {
                Queue<tbl_question> qlist= (Queue<tbl_question>) TempData["questions"];
                if(qlist.Count>0)
                {
                    q = qlist.Peek();
                    qlist.Dequeue();
                    TempData["questions"] = qlist;
                    TempData.Keep();
                }
                else
                {
                    return RedirectToAction("EndExam");
                }
            }
            else
            {
                return RedirectToAction("StudentExam");
            }


            return View(q);
            
        }
        [HttpPost]
        public ActionResult QuizStart(tbl_question q)
        {
            string correctanswer=null;
            if(q.QA!=null)
            {
                correctanswer = "A";
            }
            else if(q.QB!=null)
            {
                correctanswer = "B";
            }
            else if (q.QC != null)
            {
                correctanswer = "C";
            }
            else if (q.QD != null)
            {
                correctanswer = "D";
            }

            if(correctanswer.Equals(q.QCorrectAns))
            {
                TempData["score"] = Convert.ToInt32(TempData["score"])+1;
            }
            TempData.Keep();

            return RedirectToAction("QuizStart");
        }
        public ActionResult EndExam()
        {
            return View();
        }
        public ActionResult Dashboard()
        {
            if (Session["ad_id"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
        [HttpGet]
        public ActionResult Add_Category()
        {
            
            //Session["ad_id"] = 1;
            int ad_id = Convert.ToInt32(Session["ad_id"].ToString());
            List<Models.tbl_category> catLi = db.tbl_category.Where(x=>x.cat_fk_ad_id== ad_id).OrderByDescending(x => x.cat_id).ToList();
            ViewData["list"] = catLi;
            return View();
        }
        [HttpPost]
        public ActionResult Add_Category(Models.tbl_category cat)
        {
            
            List<Models.tbl_category> catLi = db.tbl_category.OrderByDescending(XmlSiteMapProvider => XmlSiteMapProvider.cat_id).ToList();
            ViewData["list"] = catLi;

            Random r=new Random();
            Models.tbl_category c=new Models.tbl_category();
            c.cat_name = cat.cat_name;
            c.cat_encryptedstring = crypto.Encrypt(cat.cat_name.Trim()+r.Next().ToString(), true);
            c.cat_fk_ad_id = Convert.ToInt32(Session["ad_id"].ToString());
            db.tbl_category.Add(c);
            db.SaveChanges();

            return RedirectToAction("Add_Category");
            
        }
        [HttpGet]
        public ActionResult Addquestion()
        {
            
            int sid = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> li = db.tbl_category.Where(x => x.cat_fk_ad_id == sid).ToList();
            ViewBag.list = new SelectList(li, "cat_id", "cat_name");
            return View();
        }
        [HttpPost]
        public ActionResult Addquestion(tbl_question q)
        {
            int sid = Convert.ToInt32(Session["ad_id"]);
            List<tbl_category> li = db.tbl_category.Where(x => x.cat_fk_ad_id == sid).ToList();
            ViewBag.list = new SelectList(li, "cat_id", "cat_name");
            db.tbl_question.Add(q);
            db.SaveChanges();
            TempData["msg"] = "Question Added Successfully";
            TempData.Keep();
            return RedirectToAction("Addquestion");
            
        }
        public ActionResult Index()
        {
            if (Session["ad_id"]!=null)
            {
                return RedirectToAction("Dashboard");
            }
            else if (Session["stud_id"] != null)
            {
                return RedirectToAction("StudentExam");
            }
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}