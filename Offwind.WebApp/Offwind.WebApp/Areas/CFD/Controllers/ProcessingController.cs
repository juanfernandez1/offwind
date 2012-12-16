﻿using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Xml.Serialization;
using Offwind.OpenFoam.Sintef;
using Offwind.WebApp.Controllers;
using Offwind.WebApp.Infrastructure;
using Offwind.WebApp.Models;
using Offwind.WebApp.Models.Jobs;

namespace Offwind.WebApp.Areas.CFD.Controllers
{
    public class ProcessingController : __BaseCfdController
    {
        public ProcessingController()
        {
            rootPath = "C:\\work\\temp"; // just for tests, take value from Web.config
            SectionTitle = "Processing";
        }

        public ActionResult Settings()
        {
            ShortTitle = "Settings";
            return View();
        }

        public ActionResult Simulation()
        {
            ShortTitle = "Simulation";
            ViewBag.IsInProgress = false;
            return View();
        }

        public JsonResult SimulationStart()
        {
            var job = new Job
            {
                Id = Guid.NewGuid(),
                Started = DateTime.UtcNow,
                Owner = User.Identity.Name,
                Name = StandardCases.CfdCase,
                State = JobState.Started,                
            };

            var jobZip = CreateJobPath(job);
            var jobPath = jobZip.Replace(".zip", "");
            
            var solverData = GetSolverData();            
            solverData.MakeJobFS(jobPath);
            SharpZipUtils.CompressFolder(jobPath, jobZip, null);

            new JobsController().AddJobManually(job);
            return Json("Simulation successfully started");
        }

        public JsonResult SimulationStop()
        {
            return Json("Simulation stopped");
        }

        public FileResult GetInputData(string id)
        {
            if (id == null) throw new ArgumentNullException("id");

            Guid jobId;
            if (!Guid.TryParse(id, out jobId))
            {
                _log.ErrorFormat("Unable to parse job ID: {0}", id);
                throw new ArgumentException("Invalid ID");
            }

            var job = new JobsController().GetJob(jobId);
            var fileName = String.Format("{0}.zip", job.Id);

            var isTestMode = bool.Parse(WebConfigurationManager.AppSettings["TestMode"]);
            if (isTestMode)
            {
                return File(CreateTestJobPath(), "application/octet-stream", fileName);
            }
            var path = CreateJobPath(job);
            return File(path, "application/octet-stream", fileName);
        }

        private string CreateJobPath(Job job)
        {
            Contract.Requires(job != null);
            Contract.Requires(job.Id != Guid.Empty);
            Contract.Requires(job.Owner != null);
            Contract.Requires(job.Owner.Trim().Length > 0);
            Contract.Requires(job.Owner.Trim().Length == job.Owner.Length); // No pre- and post- spaces

            string path = WebConfigurationManager.AppSettings["UsersDir"];
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, job.Owner);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, job.Id.ToString());
            path += ".zip";
            return path;
        }

        private string CreateTestJobPath()
        {
            string path = WebConfigurationManager.AppSettings["UsersDir"];
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "test");
            path += ".zip";
            return path;
        }
    }
}