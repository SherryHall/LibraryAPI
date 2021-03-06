﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LibraryAPI.Models;
using LibraryAPI.Services;

namespace LibraryAPI.Services
{
	public class BookService
	{

		public static List<Book> Books { get; set; } = new List<Book>();

		public static List<Book> BuildBookList(string currQuery)
		{
			Books.Clear();
			var connectionStrings = @"Server=localhost\SQLEXPRESS;Database=Libary;Trusted_Connection=True;";
			using (var connection = new SqlConnection(connectionStrings))
			{
				using (var cmd = new SqlCommand())
				{
					cmd.Connection = connection;
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = currQuery;

					connection.Open();
					var reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						var id = reader[0];
						var title = reader[1];
						var author = reader[2];
						var genre = reader[3];
						//var yearPublished = reader.IsDBNull(4) ?  0 : reader[4];
						var yearPublished = 0;
						var checkedOut = reader.GetBoolean(5);
						DateTime? checkOutDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
						DateTime? dueBackDate = reader.IsDBNull(7) ? (DateTime?)null : reader.GetDateTime(7);

						var book = new Book
						{
							Id = (int)id,
							Title = title as string,
							Author = author as string,
							Genre = genre as string,
							Year_Published = yearPublished,
							IsCheckedOut = checkedOut,
							LastCheckedOutDate = checkOutDate,
							DueBackDate = dueBackDate

						};
						Books.Add(book);
					}
					connection.Close();
				}
				return Books;
			}

		}
		public static List<Book> GetAllBooks()
		{
			Books = BuildBookList(@"SELECT * from Catalog");
			return Books;
		}

		public static List<Book> GetBooksByStatus(int requestedStatus)
		{
			Books = BuildBookList($@"SELECT * from Catalog WHERE IsCheckedOut = {requestedStatus}");
			return Books;
		}

		public static string AddBook(string title, string author, string genre, int yearPublished)
		{
			var connectionStrings = @"Server=localhost\SQLEXPRESS;Database=Libary;Trusted_Connection=True;";
			using (var connection = new SqlConnection(connectionStrings))
			{
				using (var cmd = new SqlCommand())
				{
					cmd.Connection = connection;
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = @"INSERT INTO Catalog (Title, Author, Genre, Year_Published)" +
										"Values (@Title, @Author, @Genre, @Year_Published)";

					cmd.Parameters.AddWithValue("@Title", title);
					cmd.Parameters.AddWithValue("@Author", author);
					cmd.Parameters.AddWithValue("@Genre", genre);
					cmd.Parameters.AddWithValue("@Year_Published", yearPublished);

					connection.Open();
					var rowsAffected = cmd.ExecuteNonQuery();
					connection.Close();

					if (rowsAffected > 0)
					{
						return "Your Book was Added";
					}
					else
					{
						return "The Insert for your Book Failed!";
					}
				}
			}
		}

		public static string UpdateBook(int id, string title, string author, string genre, int yearPublished, int checkedOut, DateTime checkOutDate, DateTime dueDate)
		{

			var connectionStrings = @"Server=localhost\SQLEXPRESS;Database=Libary;Trusted_Connection=True;";
			using (var connection = new SqlConnection(connectionStrings))
			{
				using (var cmd = new SqlCommand())
				{
					cmd.Connection = connection;
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = @"UPDATE Catalog SET Title=@Title, Author=@Author, Genre=@Genre, Year_Published=@Year_Published, LastCheckedOutDate=@LastCheckedOutDate, DueBackDate=@DueBackDate " +
										"WHERE Id = @Id";

					cmd.Parameters.AddWithValue("@Title", title);
					cmd.Parameters.AddWithValue("@Author", author);
					cmd.Parameters.AddWithValue("@Genre", genre);
					cmd.Parameters.AddWithValue("@Year_Published", yearPublished);
					cmd.Parameters.AddWithValue("@IsCheckedOut", checkedOut);
					cmd.Parameters.AddWithValue("@LastCheckedOutDate", checkOutDate.Date);
					cmd.Parameters.AddWithValue("@DueBackDate", dueDate.Date);
					cmd.Parameters.AddWithValue("@Id", id);

					connection.Open();
					var rowsAffected = cmd.ExecuteNonQuery();
					connection.Close();

					if (rowsAffected > 0)
					{
						return "Your Book was Updated";
					}
					else
					{
						return "The Update for your Book Failed!";
					}
				}
			}
		}

		public static string UpdateCheckout(int id, int checkedOut, DateTime? outDate, DateTime? dueDate, string action)
		{
			var connectionStrings = @"Server=localhost\SQLEXPRESS;Database=Libary;Trusted_Connection=True;";
			using (var connection = new SqlConnection(connectionStrings))
			{
				using (var cmd = new SqlCommand())
				{
					cmd.Connection = connection;
					cmd.CommandType = System.Data.CommandType.Text;
					cmd.CommandText = @"UPDATE Catalog SET IsCheckedOut=@IsCheckedOut, LastCheckedOutDate=@LastCheckedOutDate, DueBackDate=@DueBackDate " +
										"WHERE Id = @Id";

					cmd.Parameters.AddWithValue("@IsCheckedOut", checkedOut);
					cmd.Parameters.AddWithValue("@LastCheckedOutDate", outDate);
					cmd.Parameters.AddWithValue("@DueBackDate", dueDate);
					cmd.Parameters.AddWithValue("@Id", id);

					connection.Open();
					var rowsAffected = cmd.ExecuteNonQuery();
					connection.Close();

					if (rowsAffected > 0)
					{
						return $"Requested Book has been Checked {action}";
					}
					else
					{
						return "The Update for your Book Failed!";
					}
				}
			}
		}

		public static string CheckInOrOut(int id, string action)
		{
			// Get current info for the book requested
			string message = String.Empty;
			Books = BuildBookList($@"SELECT * from Catalog WHERE Id = {id}");
			if (Books.Count < 1)
			{
				// The book was not found. Set a message and do nothing else
				message = "Requested Book Does Not Exist";
			}
			else

			if (action.ToLower().Equals("in"))
			{
				// Check in the book. Set IsCheckedOut to false and update book
				var checkedOut = 0;
				var outDate = Books[0].LastCheckedOutDate;
				var dueDate = Books[0].DueBackDate;
				message = UpdateCheckout(id, checkedOut, outDate, dueDate, action);
			}
			else if (action.ToLower().Equals("out"))

				// Checkout requested.  If the book is already checked out, return a message, otherwide update the database to checkout book
				if (Books[0].IsCheckedOut)
				{
					message = $"{Books[0].Title} is already checked out. Return date is {Books[0].DueBackDate})";
				}
				else
				{

					// Check out the book. Set IsCheckedOut to false and update book
					var checkedOut = 1;
					var outDate = DateTime.Today.Date;
					var dueDate = outDate.AddDays(10);
					message = UpdateCheckout(id, checkedOut, outDate, dueDate, action);
				}
			else
			{
				// the action requested was neither IN nor OUT, so it is invalid
				message = "Invalid Checkout Action Requested";
			}
			return message;
		}


public static string DeleteBook(int id)
{
	var connectionStrings = @"Server=localhost\SQLEXPRESS;Database=Libary;Trusted_Connection=True;";
	using (var connection = new SqlConnection(connectionStrings))
	{
		using (var cmd = new SqlCommand())
		{
			cmd.Connection = connection;
			cmd.CommandType = System.Data.CommandType.Text;
			cmd.CommandText = @"DELETE FROM Catalog WHERE Id = @id";

			cmd.Parameters.AddWithValue("@Id", id);

			connection.Open();
			var rowsAffected = cmd.ExecuteNonQuery();
			connection.Close();

			if (rowsAffected > 0)
			{
				return "Your Book was Deleted";
			}
			else
			{
				return "The Deletion of your Book Failed!";
			}
		}
	}
}
	}
}