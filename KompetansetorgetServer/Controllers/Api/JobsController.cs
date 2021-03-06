﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.UI.WebControls.WebParts;
using KompetansetorgetServer.Models;
using Microsoft.Ajax.Utilities;

namespace KompetansetorgetServer.Controllers.Api
{
    //[Authorize]
    public class JobsController : ApiController
    {
        private KompetansetorgetServerContext db = new KompetansetorgetServerContext();

        // [HttpGet, Route("api/jobs")]
        // Activates the correct method based on query string parameters.
        // At the moment you can only use a combination of different strings if not combined with sortBy.
        /// Due to a realisation of need, all jobs will be returned with fields clogo and cname regardless of if the call ask for it or not
        //[AllowAnonymous]
        public IQueryable Get(string types = "", [FromUri] string[] studyGroups = null, string locations = "",
            string titles = "", string sortBy = "", [FromUri] string[] fields = null)
        {
            if ((studyGroups.Length != 0 && !types.IsNullOrWhiteSpace()) ||
                (studyGroups.Length != 0 && !locations.IsNullOrWhiteSpace()) ||
                (!types.IsNullOrWhiteSpace() && !locations.IsNullOrWhiteSpace()))
            {
                IQueryable<Job> jobs = GetJobsByMultiFilter(types, studyGroups, locations);
                
                if (sortBy.Equals("publish") || sortBy.Equals("-publish")
                    || sortBy.Equals("-expirydate") || sortBy.Equals("expirydate"))
                {
                    return GetJobsSorted(jobs, sortBy);
                }

                /*
                if (fields.Length == 2)
                {
                    if (fields[0].Equals("cname") && fields[1].Equals("clogo"))
                    {
                        return GetSerializedWithFields(jobs);
                    }
                }
                return GetJobsSerialized(jobs);*/
            }

            if (!types.IsNullOrWhiteSpace())
            {
                IQueryable<Job> jobs = GetJobsByType(types);
                if (sortBy.Equals("publish") || sortBy.Equals("-publish")
                    || sortBy.Equals("-expirydate") || sortBy.Equals("expirydate"))
                {
                   return GetJobsSorted(jobs, sortBy);
                }
                return GetJobsSerialized(jobs);

            }

            if (studyGroups.Length != 0)
            {
               // int i = studyGroups.Length;
                IQueryable<Job> jobs = GetJobsByStudy(studyGroups);
                if (sortBy.Equals("publish") || sortBy.Equals("-publish")
                    || sortBy.Equals("-expirydate") || sortBy.Equals("expirydate"))
                {
                    return GetJobsSorted(jobs, sortBy);
                }
                return GetJobsSerialized(jobs);
            }

            if (!locations.IsNullOrWhiteSpace())
            {
                IQueryable<Job> jobs = GetJobsByLocation(locations);
                if (sortBy.Equals("publish") || sortBy.Equals("-publish")
                    || sortBy.Equals("-expirydate") || sortBy.Equals("expirydate"))
                {
                    return GetJobsSorted(jobs, sortBy);
                }
                return GetJobsSerialized(jobs);
            }

            if (!titles.IsNullOrWhiteSpace())
            {
                IQueryable<Job> jobs = GetJobsByTitle(titles);
                if (sortBy.Equals("publish") || sortBy.Equals("-publish")
                    || sortBy.Equals("-expirydate") || sortBy.Equals("expirydate"))
                {
                    return GetJobsSorted(jobs, sortBy);
                }
                return GetJobsSerialized(jobs); 
            }

            if (fields.Length == 2)
            {
                if (!fields[0].Equals("cname") || !fields[1].Equals("clogo"))
                {
                    return GetJobs();
                }
                // int i = studyGroups.Length;
                return GetJobsWithFields(fields);
            }

            if (sortBy.Equals("publish") || sortBy.Equals("-publish")
                || sortBy.Equals("-expirydate") || sortBy.Equals("expirydate"))
            {
                return GetJobsSorted(sortBy);
            }
            return GetJobs();
        }

        //[AllowAnonymous]
        [HttpGet, Route("api/v1/jobs/lastmodified")]
        [ResponseType(typeof (Job))]
        public async Task<IHttpActionResult> GetLastModified(string types = "", [FromUri] string[] studyGroups = null,
            string locations = "",
            string titles = "")
        {
            if (studyGroups != null)
            {
                if ((studyGroups.Length != 0 && !types.IsNullOrWhiteSpace()) ||
                    (studyGroups.Length != 0 && !locations.IsNullOrWhiteSpace()) ||
                    (!types.IsNullOrWhiteSpace() && !locations.IsNullOrWhiteSpace()))
                {
                    IQueryable<Job> jobs = GetJobsByMultiFilter(types, studyGroups, locations);
                    return await SerializeLastModified(jobs);
                }
                if (studyGroups.Length != 0)
                {
                    // int i = studyGroups.Length;
                    IQueryable<Job> jobs = GetJobsByStudy(studyGroups);
                    return await SerializeLastModified(jobs);
                }
            }

            if (!types.IsNullOrWhiteSpace())
            {
                IQueryable<Job> jobs = GetJobsByType(types);
                return await SerializeLastModified(jobs);
            }

            if (!locations.IsNullOrWhiteSpace())
            {
                IQueryable<Job> jobs = GetJobsByLocation(locations);
                return await SerializeLastModified(jobs);
            }

            if (!titles.IsNullOrWhiteSpace())
            {
                IQueryable<Job> jobs = GetJobsByTitle(titles);
                return await SerializeLastModified(jobs);
            }

            // bad code, fix later if time
            var results = from job in db.jobs select job;
            List<Job> jobList = results.OrderByDescending(j => j.published).ToList();
            StringBuilder sb = new StringBuilder();
            foreach (var job in jobList)
            {
                sb.Append(job.uuid);
            }
            string hash = CalculateMD5Hash(sb.ToString());
            Job jobLast = db.jobs.OrderByDescending(j => j.modified).First();
            int count = db.jobs.Count();
            string amountOfJobs = count.ToString();
            return Ok(new
            {
                jobLast.uuid,
                jobLast.modified,
                amountOfJobs,
                hash
            });
            /*
            int amountOfJobs = db.jobs.Count();
            
            return Ok(new
            {
                    jobLast.uuid,
                    jobLast.modified,
                    amountOfJobs
            });
            */

        }

        private async Task<IHttpActionResult> SerializeLastModified(IQueryable<Job> unserializedJobs)
            {
            // bad code, fix later if time

            int count = 0;
            try
            {
                var jobLast = unserializedJobs.OrderByDescending(j => j.modified).First();
                List<Job> jobs = unserializedJobs.OrderByDescending(j => j.published).ToList();
                count = jobs.Count;
                StringBuilder sb = new StringBuilder();
                foreach (var job in jobs)
                {
                    sb.Append(job.uuid);
                }
                string hash = CalculateMD5Hash(sb.ToString());
                string amountOfJobs = count.ToString();
                return Ok(new
                {
                    jobLast.uuid,
                    jobLast.modified,
                    amountOfJobs,
                    hash
                });
            }
            catch
            {
                string amountOfJobs = count.ToString();
                return Ok(new
                {amountOfJobs});
            }

        }
    
        // GET: api/Jobs
        //public IQueryable<Job> GetJobs()
        /// <summary>
        /// This method is called if no query strings are presented
        /// </summary>
        /// <returns></returns>
        private IQueryable GetJobs()
        {
            var jobs = from job in db.jobs select job;
            return GetJobsSerialized(jobs);
        }

        // GET: api/Jobs/5
        // Example: /api/jobs/2c70edff-edbe-4d6d-8e79-10a47f330feb
        // [Authorize(Users=id)]
        [HttpGet, Route("api/v1/jobs/{id}")]
        [ResponseType(typeof (Job))]
        public async Task<IHttpActionResult> GetJob(string id, string minNot = "")
        {
            Job job = await db.jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }


            if (!minNot.Equals("true"))
            {
                //return Ok(job);
                return Ok(new
                {
                    job.uuid,
                    job.title,
                    //job.description,
                    job.webpage,
                    job.linkedInProfile,
                    job.expiryDate,
                    job.stepsToApply,
                    job.created,
                    job.published,
                    job.modified,
                    companies = job.companies.Select(c => new {c.id, c.name, c.logo }),
                    contacts = job.contacts.Select(c => new {c.id}),
                    locations = job.locations.Select(l => new {l.id}),
                    jobTypes = job.jobTypes.Select(jt => new {jt.id}),
                    studyGroups = job.studyGroups.Select(st => new {st.id})
                });
            }
            else
            {
                return Ok(new
                {
                    job.uuid,
                    job.title,
                    job.webpage,
                    job.published,
                    job.modified,
                    job.expiryDate,
                    companies = job.companies.Select(c => new {c.id, c.name, c.logo})
                });
            }
        }

        /// <summary>
        /// Lists all jobs that contains that spesific Location. 
        /// GET: api/jobs?locations=vestagder
        /// GET: api/jobs?locations=austagder
        /// </summary>
        /// <param name="locations">the locations identificator</param>
        /// <returns></returns> 
        private IQueryable<Job> GetJobsByLocation(string locations = "")
        {
            var jobs = from job in db.jobs
                       where job.locations.Any(l => l.id.Equals(locations))
                       select job;

            return jobs;
        }

        /// <summary>
        /// Lists all jobs that contains that spesific StudyGroup. 
        /// GET: api/jobs?studyGroups=datateknologi
        /// GET: api/jobs?studyGroups=idrettsfag
        /// Also supports combinations:
        /// GET: api/jobs/?studygroups=idrettsfag&studygroups=lærerutdanning
        /// </summary>
        /// <param name="studyGroups">the StudyGroup identificator</param>
        /// <returns></returns> 
       // [HttpGet, Route("api/jobs")]
        private IQueryable<Job> GetJobsByStudy(string[] studyGroups = null)
        {
            var jobs = from job in db.jobs               
                       where job.studyGroups.Any(s => studyGroups.Contains(s.id))
                       select job;

            return jobs;
        }

        /// <summary>
        /// List all jobs that contain that exact title (could be improved upon)
        /// </summary>
        /// <param name="title"></param>
        /// <returns></returns>
        private IQueryable<Job> GetJobsByTitle(string titles)
        {
            // White space Delimiter + removed.
            titles = titles.Replace("+", " ");
            var jobs = from job in db.jobs               
                       where job.title.Contains(titles)
                       select job;
            
            return jobs;
        }
    

        /// <summary>
        /// Lists all jobs that contains that spesific JobType (full time and part time jobs). 
        /// GET: api/jobs?types=heltid
        /// GET: api/jobs?types=deltid
        /// </summary>
        /// <param name="types">the jobTypes identificator</param>
        /// <returns></returns> 
       // [HttpGet, Route("api/jobs")]
        private IQueryable<Job> GetJobsByType(string types = "")
        {
            var jobs = from job in db.jobs               
                       where job.jobTypes.Any(jt => jt.id.Equals(types))
                       select job;

            return jobs;
        }

        /// <summary>
        /// Lists all jobs with the respective companies names and logo. 
        /// GET: api/jobs?fields=cname&fields=clogo
        /// </summary>
        private IQueryable GetJobsWithFields(String[] fields)
        {
            var jobs = from job in db.jobs select job;

            return jobs.Select(j => new
            {
                j.uuid,
                j.title,
                //j.description,  // No need for this field atm, + very much data
                j.webpage,
                j.linkedInProfile,
                j.expiryDate,
                j.stepsToApply,
                j.created,
                j.published,
                j.modified,
                companies = j.companies.Select(c => new {c.id, c.name, c.logo}),
                locations = j.locations.Select(l => new {l.id}),
                jobTypes = j.jobTypes.Select(jt => new {jt.id}),
                studyGroups = j.studyGroups.Select(st => new {st.id})
            });
        }

        private IQueryable<Job> GetJobsByMultiFilter(string types = "", [FromUri] string[] studyGroups = null, string locations = "")
        {
           
            IQueryable<Job> jobs = null;
            if (studyGroups.Length != 0 && !types.IsNullOrWhiteSpace() && !locations.IsNullOrWhiteSpace())
            {
                jobs = from job in db.jobs
                           where job.studyGroups.Any(s => studyGroups.Contains(s.id))
                           where job.locations.Any(l => l.id.Equals(locations))
                           where job.jobTypes.Any(jt => jt.id.Equals(types))
                           select job;
            }

            else if (!types.IsNullOrWhiteSpace() && !locations.IsNullOrWhiteSpace())
            {
                jobs = from job in db.jobs
                           where job.locations.Any(l => l.id.Equals(locations))
                           where job.jobTypes.Any(jt => jt.id.Equals(types))
                           select job;
            }

            else if (studyGroups.Length != 0 && !locations.IsNullOrWhiteSpace())
            {
                jobs = from job in db.jobs
                           where job.studyGroups.Any(s => studyGroups.Contains(s.id))
                           where job.locations.Any(l => l.id.Equals(locations))
                           select job;
            }

            else if (studyGroups.Length != 0 && !types.IsNullOrWhiteSpace())
            {
                jobs = from job in db.jobs
                           where job.studyGroups.Any(s => studyGroups.Contains(s.id))
                           where job.jobTypes.Any(jt => jt.id.Equals(types))
                           select job;
            }

            return jobs;
        }
        /// <summary>
        /// Serializes the job object for json.
        /// Also adds company name and logo
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns> 
        private IQueryable GetSerializedWithFields(IQueryable<Job> jobs)
        {
            return jobs.Select(j => new
            {
                j.uuid,
                j.title,
                //j.description,  // No need for this field atm, + very much data
                j.webpage,
                j.linkedInProfile,
                j.expiryDate,
                j.stepsToApply,
                j.created,
                j.published,
                j.modified,
                companies = j.companies.Select(c => new { c.id, c.name, c.logo}),
                locations = j.locations.Select(l => new { l.id }),
                jobTypes = j.jobTypes.Select(jt => new { jt.id }),
                studyGroups = j.studyGroups.Select(st => new { st.id })
            });
        }


        /// <summary>
        /// Serializes the job object for json.
        /// </summary>
        /// <param name="jobs"></param>
        /// <returns></returns> 
        private IQueryable GetJobsSerialized(IQueryable<Job> jobs)
        {
            return jobs.Select(j => new
            {
                j.uuid,
                j.title,
                //j.description,  // No need for this field atm, + very much data
                j.webpage,
                j.linkedInProfile,
                j.expiryDate,
                j.stepsToApply,
                j.created,
                j.published,
                j.modified,
                companies = j.companies.Select(c => new { c.id, c.name, c.logo }),
                locations = j.locations.Select(l => new { l.id }),
                jobTypes = j.jobTypes.Select(jt => new { jt.id }),
                studyGroups = j.studyGroups.Select(st => new { st.id })
            });
        }

        /// <summary>
        /// List jobs in a ascending or descending order based on sortBy parameter.
        /// Examples for use:
        /// GET: api/jobs/?location=vestagder&sortby=publish (oldest to newest)
        /// GET: api/jobs/?location=vestagder&sortby=-publish (newest to oldest)
        /// GET: api/jobs/?type=deltid&sortby=expirydate (oldest to newest)
        /// GET: api/jobs/?type=deltid&sortby=-expirydate (newest to oldest)
        /// </summary>
        /// <param name="queryResult">A result of a query in table Jobs</param>
        /// <param name="sortBy">published = the date a job was published.
        ///                      expirydate = the last date to apply for the job.
        ///                      -published = newest to oldest
        ///                      -expirydate = newest to oldest.</param>
        /// <returns></returns>
        private IQueryable GetJobsSorted(IQueryable<Job> queryResult, string sortBy = "")
        {
            var jobs = queryResult.Select(j => new
            {
                j.uuid,
                j.title,
                //j.description,  // No need for this field atm, + very much data
                j.webpage,
                j.linkedInProfile,
                j.expiryDate,
                j.stepsToApply,
                j.created,
                j.published,
                j.modified,
                companies = j.companies.Select(c => new { c.id, c.name, c.logo }),
                locations = j.locations.Select(l => new { l.id }),
                jobTypes = j.jobTypes.Select(jt => new { jt.id }),
                studyGroups = j.studyGroups.Select(st => new { st.id })
            });

            switch (sortBy)
            {
                case "expirydate":
                    jobs = jobs.OrderByDescending(j => j.expiryDate);
                    return jobs;
                case "publish":
                    jobs = jobs.OrderByDescending(j => j.published);
                    return jobs;

                case "-expirydate":
                    jobs = jobs.OrderBy(j => j.expiryDate);
                    return jobs;
                case "-publish":
                    jobs = jobs.OrderBy(j => j.published);
                    return jobs;

                default:
                    return GetJobs();
            }
            /*
                if (orderBy.Equals("desc"))
                {
                    switch (sortBy)
                    {
                        case "expirydate":
                            jobs = jobs.OrderByDescending(j => j.expiryDate);
                            return jobs;
                        case "published":
                            jobs = jobs.OrderByDescending(j => j.published);
                            return jobs;

                        default:
                            return GetJobs();
                    }
                }

                switch (sortBy)
                {
                    case "expirydate":
                        jobs = jobs.OrderBy(j => j.expiryDate);
                        return jobs;
                    case "published":
                        jobs = jobs.OrderBy(j => j.published);
                        return jobs;

                    default:
                        return GetJobs();
                }

                */
            }

        /// <summary>
        /// List jobs in a ascending or descending order based on sortBy parameter.
        /// GET: api/jobs/?sortby=published oldest to newest
        /// GET: api/jobs/?sortby=-published newest to oldest
        /// GET: api/jobs/?sortby=expirydate
        /// 
        /// Deprecated, no longer working:
        /// GET: api/jobs/?sortby=locations
        /// GET: api/jobs/?sortby=studyGroups
        /// </summary>
        /// <param name="sortBy"></param>
        /// <returns></returns>
        private IQueryable GetJobsSorted(string sortBy = "")
        {

            var queryResult = from job in db.jobs select job;

            // Won't work due to incompatible return type.
            //GetJobsSerialized(jobs1)

            var jobs = queryResult.Select(j => new
                {
                j.uuid,
                j.title,
                //j.description,  // No need for this field atm, + very much data
                j.webpage,
                j.linkedInProfile,
                j.expiryDate,
                j.stepsToApply,
                j.created,
                j.published,
                j.modified,
                companies = j.companies.Select(c => new { c.id, c.name, c.logo }),
                locations = j.locations.Select(l => new { l.id }),
                jobTypes = j.jobTypes.Select(jt => new { jt.id }),
                studyGroups = j.studyGroups.Select(st => new { st.id })
            });
            switch (sortBy)
            {
                case "expirydate":
                    jobs = jobs.OrderByDescending(j => j.expiryDate);
                    return jobs;
                case "publish":
                    jobs = jobs.OrderByDescending(j => j.published);
                    return jobs;

                case "-expirydate":
                    jobs = jobs.OrderBy(j => j.expiryDate);
                    return jobs;
                case "-publish":
                    jobs = jobs.OrderBy(j => j.published);
                    return jobs;

                default:
                    return GetJobs();
            }
        }

        /// <summary>
        /// Used to create a 32 bit hash of all the projects uuid,
        /// used as part of the cache strategy.
        /// This is not to create a safe encryption, but to create a hash that im
        /// certain that the php backend can replicate.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private string CalculateMD5Hash(string input)
        {
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }


        // PUT: api/Jobs/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutJob(string id, Job job)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != job.uuid)
            {
                return BadRequest();
            }

            db.Entry(job).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!JobExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Jobs
        [ResponseType(typeof(Job))]
        public async Task<IHttpActionResult> PostJob(Job job)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.jobs.Add(job);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (JobExists(job.uuid))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = job.uuid }, job);
        }

        // DELETE: api/Jobs/5
        [ResponseType(typeof(Job))]
        public async Task<IHttpActionResult> DeleteJob(string id)
        {
            Job job = await db.jobs.FindAsync(id);
            if (job == null)
            {
                return NotFound();
            }

            db.jobs.Remove(job);
            await db.SaveChangesAsync();

            return Ok(job);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool JobExists(string id)
        {
            return db.jobs.Count(e => e.uuid == id) > 0;
        }
    }
}