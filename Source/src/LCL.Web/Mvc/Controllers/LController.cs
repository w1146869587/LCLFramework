﻿using LCL.Bus;
using LCL.Domain.Entities;
using LCL.Domain.Repositories;
using LCL.Domain.Services;
using LCL.Infrastructure;
using LCL.Web.Configuration;
using LCL.Web.Security;
using LCL.Web.Template;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web.Mvc;
using System.Web.Security;

namespace LCL.Web.Mvc.Controllers
{
    [LCLHttpsRequirement(SslRequirement.No)]
    [TemplateRelevant]
    public class LController : Controller
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        public ILclMvcConfiguration LclMvcConfiguration { get; set; }
        public IEventBus EventBus { get; set; }
        protected LController()
        {
            ModelBinders.Binders.DefaultBinder = new LclModelBinder();
        }
        /// <summary>
        /// 获取服务。
        /// </summary>
        public TService Service<TService>() where TService : class
        {
            if (!_services.ContainsKey(typeof(TService)))
            {
                _services[typeof(TService)] = EngineContext.Current.Resolve<TService>();
            }
            return (TService)_services[typeof(TService)];
        }
        public IRepository<TAggregateRoot> Repository<TAggregateRoot>() where TAggregateRoot : class, IAggregateRoot
        {
            var repo = Service<IRepository<TAggregateRoot>>();
            return repo;
        }
        public ActionResult ChangeTemplate(string template)
        {
            TemplateService.DefaultTemplateName = template ?? TemplateService.DefaultTemplateName;

            return this.RedirectToAction("Index");
        }
        public ActionResult PageNotFound()
        {
            return View("PageNotFound");
        }
        protected ActionResult AccessDeniedView()
        {
            //return new HttpUnauthorizedResult();
            return RedirectToAction("AccessDenied", "Security", new { pageUrl = this.Request.RawUrl });
        }

        #region L
        protected virtual string L(string name)
        {
            return RF.Localization.GetString(name);
        }
        protected string L(string name, params object[] args)
        {
            return RF.Localization.GetString(name, args);
        }
        protected virtual string L(string name, CultureInfo culture)
        {
            return RF.Localization.GetString(name, culture);
        }
        protected string L(string name, CultureInfo culture, params object[] args)
        {
            return RF.Localization.GetString(name, culture, args);
        }
        #endregion

        #region RenderPartialViewToString
        public virtual string RenderPartialViewToString()
        {
            return RenderPartialViewToString(null, null);
        }
        public virtual string RenderPartialViewToString(string viewName)
        {
            return RenderPartialViewToString(viewName, null);
        }
        public virtual string RenderPartialViewToString(object model)
        {
            return RenderPartialViewToString(null, model);
        }
        public virtual string RenderPartialViewToString(string viewName, object model)
        {
            //Original source code: http://craftycodeblog.com/2010/05/15/asp-net-mvc-render-partial-view-to-string/
            if (string.IsNullOrEmpty(viewName))
                viewName = this.ControllerContext.RouteData.GetRequiredString("action");

            this.ViewData.Model = model;

            using (var sw = new StringWriter())
            {
                ViewEngineResult viewResult = System.Web.Mvc.ViewEngines.Engines.FindPartialView(this.ControllerContext, viewName);
                var viewContext = new ViewContext(this.ControllerContext, viewResult.View, this.ViewData, this.TempData, sw);
                viewResult.View.Render(viewContext, sw);

                return sw.GetStringBuilder().ToString();
            }
        }
        protected void LogException(Exception exc)
        {
            Logger.LogError("LCL Error: ", exc);
        }
        /// <summary>
        /// Display success notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void SuccessNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Success, message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void ErrorNotification(string message, bool persistForTheNextRequest = true)
        {
            AddNotification(NotifyType.Error, message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display error notification
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether exception should be logged</param>
        protected virtual void ErrorNotification(Exception exception, bool persistForTheNextRequest = true, bool logException = true)
        {
            if (logException)
                LogException(exception);
            AddNotification(NotifyType.Error, exception.Message, persistForTheNextRequest);
        }
        /// <summary>
        /// Display notification
        /// </summary>
        /// <param name="type">Notification type</param>
        /// <param name="message">Message</param>
        /// <param name="persistForTheNextRequest">A value indicating whether a message should be persisted for the next request</param>
        protected virtual void AddNotification(NotifyType type, string message, bool persistForTheNextRequest)
        {
            string dataKey = string.Format("LCL.notifications.{0}", type);
            if (persistForTheNextRequest)
            {
                if (TempData[dataKey] == null)
                    TempData[dataKey] = new List<string>();
                ((List<string>)TempData[dataKey]).Add(message);
            }
            else
            {
                if (ViewData[dataKey] == null)
                    ViewData[dataKey] = new List<string>();
                ((List<string>)ViewData[dataKey]).Add(message);
            }
        }
        #endregion

        #region Private Constants
        private const string SUCCESS_PAGE_ACTION = "SuccessPage";
        private const string SUCCESS_PAGE_CONTROLLER = "Home";
        #endregion

        #region Protected Methods
        /// <summary>
        /// 将页面重定向到成功页面。
        /// </summary>
        /// <param name="pageTitle">需要在成功页面显示的成功信息。</param>
        /// <param name="action">成功信息显示后返回的Action名称。默认值：Index。</param>
        /// <param name="controller">成功信息显示后返回的Controller名称。默认值：Home。</param>
        /// <param name="waitSeconds">在成功页面停留的时间（秒）。默认值：3。</param>
        /// <returns>执行的Action Result。</returns>
        protected ActionResult RedirectToSuccess(string pageTitle, string action = "Index", string controller = "Home", int waitSeconds = 3)
        {
            return this.RedirectToAction(SUCCESS_PAGE_ACTION, SUCCESS_PAGE_CONTROLLER, new { pageTitle = pageTitle, retAction = action, retController = controller, waitSeconds = waitSeconds });
        }

        #endregion

    }
}

