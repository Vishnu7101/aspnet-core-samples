﻿using BoldReports.Web;
using BoldReports.Writer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace ReportsCoreSamples.Controllers
{
    [Route("report-writer")]
    public class ReportWriterController : PreviewController
    {
        private IWebHostEnvironment _hostingEnvironment;

        public ReportWriterController(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        internal ExternalServer Server { get; set; }
        public string ServerURL { get; set; }
        public string getName(string name)
        {
            string[] splittedNames = name.Split('-');
            string sampleName = "";
            foreach (string splittedName in splittedNames)
            {
                sampleName += (char.ToUpper(splittedName[0]) + splittedName.Substring(1));
            }
            return sampleName;
        }

        // GET: ReportWriter
        [HttpGet("")]
        public ActionResult Index()
        {
            this.updateMetaData();
            return View();
        }

        [HttpPost("generate")]
        public IActionResult Generate(string reportName, string type)
        {
            try
            {
                string basePath = _hostingEnvironment.WebRootPath;
                string fileName = reportName.Contains("-") ? getName(reportName) : (char.ToUpper(reportName[0]) + reportName.Substring(1));
                WriterFormat format;
                ReportWriter reportWriter = new ReportWriter();

                ExternalServer externalServer = new ExternalServer(_hostingEnvironment);
                this.Server = externalServer;
                this.ServerURL = "Sample";
                externalServer.ReportServerUrl = this.ServerURL;
                reportWriter.ReportingServer = this.Server;
                reportWriter.ReportServerUrl = this.ServerURL;
                reportWriter.ReportServerCredential = System.Net.CredentialCache.DefaultCredentials;
                
                reportWriter.ReportProcessingMode = ProcessingMode.Remote;
                reportWriter.ExportSettings = new customBrowsertype(_hostingEnvironment);
                reportWriter.ExportResources.BrowserType = ExportResources.BrowserTypes.External;
                reportWriter.ExportResources.ResourcePath = Path.Combine(basePath, "puppeteer");

                FileStream inputStream = new FileStream(Path.Combine(basePath, "resources", "Report", reportName + ".rdl"), FileMode.Open, FileAccess.Read);
                reportWriter.LoadReport(inputStream);

                reportWriter.ExportResources.Scripts = new List<string>
                {
                    basePath + "/scripts/bold-reports/v2.0/common/bold.reports.common.min.js",
                    basePath + "/scripts/bold-reports/v2.0/common/bold.reports.widgets.min.js",
                    //Report Viewer Script
                    basePath + "/scripts/bold-reports/v2.0/bold.report-viewer.min.js"
                };

                reportWriter.ExportResources.DependentScripts = new List<string>
                {
                    basePath + "/scripts/dependent/jquery.min.js"
                };

                if (reportWriter.FontSettings == null)
                {
                    reportWriter.FontSettings = new BoldReports.RDL.Data.FontSettings();
                }
                reportWriter.FontSettings.BasePath = Path.Combine(_hostingEnvironment.WebRootPath, "fonts");

                if (type == "pdf")
                {
                    fileName += ".pdf";
                    format = WriterFormat.PDF;
                }
                else if (type == "word")
                {
                    fileName += ".docx";
                    format = WriterFormat.Word;
                }
                else if (type == "csv")
                {
                    fileName += ".csv";
                    format = WriterFormat.CSV;
                }
                else if (type == "html")
                {
                    fileName += ".html";
                    format = WriterFormat.HTML;
                }
                else if (type == "ppt")
                {
                    fileName += ".ppt";
                    format = WriterFormat.PPT;
                }
                else if (type == "xml")
                {
                    fileName += ".xml";
                    format = WriterFormat.XML;
                }
                else
                {
                    fileName += ".xlsx";
                    format = WriterFormat.Excel;
                }
                MemoryStream memoryStream = new MemoryStream();
                reportWriter.Save(memoryStream, format);

                memoryStream.Position = 0;
                string mimeType = "application/" + type;
                FileStreamResult fileStreamResult = new FileStreamResult(memoryStream, mimeType);
                fileStreamResult.FileDownloadName = fileName;
                return fileStreamResult;
            }
            catch
            {
                return null;
            }

        }
    }

    public class customBrowsertype : ExportSettings
    {
        private IWebHostEnvironment _hostingEnvironment;

        public customBrowsertype(IWebHostEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }
        public override string GetImageFromHTML(string url)
        {
            return ConvertBase64(url).Result;
        }
        public async Task<string> ConvertBase64(string url)
        {
            string puppeteerChromeExe = "";
            puppeteerChromeExe = Path.Combine(_hostingEnvironment.WebRootPath, "puppeteer", "Win-901912", "chrome-win", "chrome.exe");
            await using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new PuppeteerSharp.LaunchOptions
            {
                Headless = true,
                Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-gpu",
                        "--disable-setuid-sandbox",
                        "--disable-accelerated-2d-canvas",
                        "--no-zygote",
                        "--single-process",
                        "--dump-blink-runtime-call-stats",
                        "--profiling-flush",
                    },
                ExecutablePath = puppeteerChromeExe
            });
            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(url);
            var result = await page.WaitForSelectorAsync("#imagejsonData").Result.GetPropertyAsync("innerText").Result.JsonValueAsync<string>();
            return result;
        }
    }
}
