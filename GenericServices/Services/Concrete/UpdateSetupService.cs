﻿#region licence
// The MIT License (MIT)
// 
// Filename: UpdateSetupService.cs
// Date Created: 2014/07/22
// 
// Copyright (c) 2014 Jon Smith (www.selectiveanalytics.com & www.thereformedprogrammer.net)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
#endregion
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using GenericLibsBase;
using GenericServices.Core;
using GenericServices.Core.Internal;

namespace GenericServices.Services.Concrete
{

    public class UpdateSetupService : IUpdateSetupService
    {
        private readonly IGenericServicesDbContext _db;

        public UpdateSetupService(IGenericServicesDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// This returns a status which, if Valid, has single entry using the primary keys to find it.
        /// </summary>
        /// <typeparam name="T">The type of the data to output. 
        /// Type must be a type either an EF data class or one of the EfGenericDto's</typeparam>
        /// <param name="keys">The keys must be given in the same order as entity framework has them</param>
        /// <returns>Status. If valid Result holds data (not tracked), otherwise null</returns>
        public ISuccessOrErrors<T> GetOriginal<T>(params object[] keys) where T : class
        {
            var service = DecodeToService<UpdateSetupService>.CreateCorrectService<T>(WhatItShouldBe.SyncClassOrSpecificDto, _db);
            return service.GetOriginal(keys);
        }
    }

    //--------------------------------
    //direct version

    public class UpdateSetupService<TEntity> : IUpdateSetupService<TEntity> where TEntity : class, new()
    {
        private readonly IGenericServicesDbContext _db;

        public UpdateSetupService(IGenericServicesDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// This gets a single entry using the lambda expression as a where part
        /// </summary>
        /// <param name="whereExpression">Should be a 'where' expression that returns one item</param>
        /// <returns>Status. If valid Result holds data (not tracked), otherwise null</returns>
        public ISuccessOrErrors<TEntity> GetOriginalUsingWhere(Expression<Func<TEntity, bool>> whereExpression)
        {
            return _db.Set<TEntity>().Where(whereExpression).AsNoTracking().RealiseSingleWithErrorChecking();
        }

        /// <summary>
        /// This finds an entry using the primary key(s) in the data
        /// </summary>
        /// <param name="keys">The keys must be given in the same order as entity framework has them</param>
        /// <returns>Status. If valid Result holds data (not tracked), otherwise null</returns>
        public ISuccessOrErrors<TEntity> GetOriginal(params object[] keys)
        {
            return GetOriginalUsingWhere(BuildFilter.CreateFilter<TEntity>(_db.GetKeyProperties<TEntity>(), keys));
        }
    }

    //--------------------------------
    //dto version

    public class UpdateSetupService<TEntity, TDto> : IUpdateSetupService<TEntity, TDto>
        where TEntity : class, new()
        where TDto : EfGenericDto<TEntity, TDto>, new()
    {
        private readonly IGenericServicesDbContext _db;

        public UpdateSetupService(IGenericServicesDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// This returns a single entry using the primary keys to find it. It also calls
        /// the dto's SetupSecondaryData to setup any extra data needed
        /// </summary>
        /// <param name="keys">The keys must be given in the same order as entity framework has them</param>
        /// <returns>Status. If valid TDto type with properties copyed over and SetupSecondaryData called 
        /// to set secondary data, otherwise null</returns>
        public ISuccessOrErrors<TDto> GetOriginal(params object[] keys)
        {
            return GetOriginalUsingWhere(BuildFilter.CreateFilter<TEntity>(_db.GetKeyProperties<TEntity>(), keys));
        }

        /// <summary>
        /// This gets a single entry using the lambda expression as a where part. It also calls
        /// the dto's SetupSecondaryData to setup any extra data needed
        /// </summary>
        /// <param name="whereExpression">Should be a 'where' expression that returns one item</param>
        /// <returns>Status. If valid TDto type with properties copyed over and SetupSecondaryData called 
        /// to set secondary data, otherwise null</returns>
        public ISuccessOrErrors<TDto> GetOriginalUsingWhere(Expression<Func<TEntity, bool>> whereExpression)
        {
            var dto = new TDto();
            if (!dto.SupportedFunctions.HasFlag(CrudFunctions.Update))
                throw new InvalidOperationException("This DTO does not support update.");

            var status = dto.DetailDtoFromDataIn(_db, whereExpression);
            if (!status.IsValid) return status;

            if (!dto.SupportedFunctions.HasFlag(CrudFunctions.DoesNotNeedSetup))
                status.Result.SetupSecondaryData(_db, status.Result);
            return status;
        }
    }
}
