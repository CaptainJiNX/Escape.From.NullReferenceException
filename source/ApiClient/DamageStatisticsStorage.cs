﻿using System.Collections.Generic;
using System.IO;

namespace ApiClient
{
	public class DamageStatisticsStorage : ISimpleStorage<DamageStatistics>
	{
		private const string StorageLocation = "DamageStatistics.csv";
		private readonly List<DamageStatistics> _statistics = new List<DamageStatistics>(); 

		public DamageStatisticsStorage()
		{
			if (!File.Exists(StorageLocation)) return;
			foreach (var line in File.ReadAllLines(StorageLocation))
			{
				_statistics.Add(new DamageStatistics(line));
			}
		}

		public IEnumerable<DamageStatistics> GetAll()
		{
			return _statistics;
		}

		public void Store(DamageStatistics value)
		{
			_statistics.Add(value);
			File.AppendAllLines(StorageLocation, new[] {value.ToString()});
		}
	}
}