﻿using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using NSSWeb.Data;
using Dapper;
using Microsoft.AspNetCore.Http;

namespace NSSWeb.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StudentsController(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection Connection
        {
            get
            {
                return new SqliteConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        // GET api/students
        [HttpGet]
        public async Task<IActionResult> Get(string q)
        {
            string sql = @"
            SELECT
                s.Id,
                s.FirstName,
                s.LastName,
                s.SlackHandle,
                c.Id,
                c.Name
            FROM Student s
            JOIN Cohort c ON s.CohortId = c.Id
            WHERE 1=1
            ";

            if (q != null)
            {
                string isQ = $@"
                    AND i.FirstName LIKE '%{q}%'
                    OR i.LastName LIKE '%{q}%'
                    OR i.SlackHandle LIKE '%{q}%'
                ";
                sql = $"{sql} {isQ}";
            }

            Console.WriteLine(sql);

            using (IDbConnection conn = Connection)
            {

                IEnumerable<Student> students = await conn.QueryAsync<Student, Cohort, Student>(
                    sql,
                    (student, cohort) =>
                    {
                        student.Cohort = cohort;
                        return student;
                    }
                );
                return Ok(students);
            }
        }

        // GET api/students/5
        [HttpGet("{id}", Name = "GetStudent")]
        public async Task<IActionResult> Get([FromRoute]int id)
        {
            string sql = $@"
            SELECT
                i.Id,
                i.FirstName,
                i.LastName,
                i.SlackHandle,
                i.Specialty,
                i.CohortId
            FROM Student i
            WHERE i.Id = {id}
            ";

            using (IDbConnection conn = Connection)
            {
                IEnumerable<Student> students = await conn.QueryAsync<Student>(sql);
                return Ok(students);
            }
        }

        // POST api/students
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Student student)
        {
            string sql = $@"INSERT INTO Student VALUES
            (
                null,
                '{student.FirstName}'
                ,'{student.LastName}'
                ,'{student.SlackHandle}'
            );
            select seq from sqlite_sequence where name='Student';";

            using (IDbConnection conn = Connection)
            {
                var newId = (await conn.QueryAsync<int>(sql)).Single();
                student.Id = newId;
                return CreatedAtRoute("GetStudent", new { id = newId }, student);
            }
        }

        // PUT api/students/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(int id, [FromBody] Student student)
        {
            string sql = $@"
            UPDATE Student
            SET FirstName = '{student.FirstName}',
                LastName = '{student.LastName}',
                SlackHandle = '{student.SlackHandle}'
            WHERE Id = {id}";

            try
            {
                using (IDbConnection conn = Connection)
                {
                    int rowsAffected = await conn.ExecuteAsync(sql);
                    if (rowsAffected > 0)
                    {
                        return new StatusCodeResult(StatusCodes.Status204NoContent);
                    }
                    throw new Exception("No rows affected");
                }
            }
            catch (Exception)
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
        }

        // DELETE api/students/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            string sql = $@"DELETE FROM Student WHERE Id = {id}";

            using (IDbConnection conn = Connection)
            {
                int rowsAffected = await conn.ExecuteAsync(sql);
                if (rowsAffected > 0)
                {
                    return new StatusCodeResult(StatusCodes.Status204NoContent);
                }
                throw new Exception("No rows affected");
            }

        }

        private bool StudentExists(int id)
        {
            string sql = $"SELECT Id FROM Student WHERE Id = {id}";
            using (IDbConnection conn = Connection)
            {
                return conn.Query<Student>(sql).Count() > 0;
            }
        }
    }
}
