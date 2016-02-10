﻿using System;
using System.Web.Http;
using KompetansetorgetServer.ContextDbs;
using KompetansetorgetServer.Models;
using System.Linq;
using System.Web.Http.Description;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Net;

namespace KompetansetorgetServer
{
	public class StudentController : ApiController
	{
		private KompetanseContext db;

		public StudentController ()
		{
			this.db = new KompetanseContext();
		}

		public IQueryable<Student> GetStudents()
		{
			return db.Students;
		}

		// GET: api/Students/5
		[ResponseType(typeof(Student))]
		public IHttpActionResult GetStudent(string id)
		{
			Student student = db.Students.Find(id);
			if (student == null)
			{
				return NotFound();
			}

			return Ok(student);
		}

		// PUT: api/Students/5
		[ResponseType(typeof(void))]
		public IHttpActionResult PutStudent(string id, Student student)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			if (id != student.Username)
			{
				return BadRequest();
			}

			db.Entry(student).State = EntityState.Modified;

			try
			{
				db.SaveChanges();
			}
			catch (DbUpdateConcurrencyException)
			{
				if (!StudentExists(id))
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

		private bool StudentExists(string id)
		{
			return db.Students.Count(e => e.Username == id) > 0;
		}
	}
}
