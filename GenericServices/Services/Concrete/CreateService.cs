﻿using GenericServices.Core;
using GenericServices.Core.Internal;

namespace GenericServices.Services.Concrete
{
    public class CreateService : ICreateService
    {
        private readonly IDbContextWithValidation _db;

        public CreateService(IDbContextWithValidation db)
        {
            _db = db;
        }

        /// <summary>
        /// This adds a new entity class to the database with error checking
        /// </summary>
        /// <typeparam name="T">The type of the data to output. 
        /// Type must be a type either an EF data class or one of the EfGenericDto's</typeparam>
        /// <param name="newItem">either entity class or dto to create the data item with</param>
        /// <returns>status</returns>
        public ISuccessOrErrors Create<T>(T newItem) where T : class
        {
            var service = DecodeToService<CreateService>.CreateCorrectService<T>(WhatItShouldBe.SyncAnything, _db);
            return service.Create(newItem);
        }
    }
    
    //-----------------------------------------------
    //direct service

    public class CreateService<TData> : ICreateService<TData> where TData : class
    {
        private readonly IDbContextWithValidation _db;

        public CreateService(IDbContextWithValidation db)
        {
            _db = db;
        }

        /// <summary>
        /// This adds a new entity class to the database with error checking
        /// </summary>
        /// <param name="newItem"></param>
        /// <returns>status</returns>
        public ISuccessOrErrors Create(TData newItem)
        {
            _db.Set<TData>().Add(newItem);
            var result = _db.SaveChangesWithValidation();
            if (result.IsValid)
                result.SetSuccessMessage("Successfully created {0}.", typeof(TData).Name);

            return result;
        }
    }

    //---------------------------------------------------------------------------
    //DTO version

    public class CreateService<TData, TDto> : ICreateService<TData, TDto>
        where TData : class, new()
        where TDto : EfGenericDto<TData, TDto>
    {
        private readonly IDbContextWithValidation _db;


        public CreateService(IDbContextWithValidation db)
        {
            _db = db;
        }

        /// <summary>
        /// This uses a dto to create a data class which it writes to the database with error checking
        /// </summary>
        /// <param name="dto">If an error then its resets any secondary data so that you can reshow the dto</param>
        /// <returns>status</returns>
        public ISuccessOrErrors Create(TDto dto)
        {
            ISuccessOrErrors result = new SuccessOrErrors();
            if (!dto.SupportedFunctions.HasFlag(ServiceFunctions.Create))
                return result.AddSingleError("Create of a new {0} is not supported in this mode.", dto.DataItemName);
            
            var tData = new TData();
            result = dto.CopyDtoToData(_db, dto, tData);    //update those properties we want to change
            if (result.IsValid)
            {
                _db.Set<TData>().Add(tData);
                result = _db.SaveChangesWithValidation();
                if (result.IsValid)
                    return result.SetSuccessMessage("Successfully created {0}.", dto.DataItemName);
            }

            //otherwise there are errors
            if (!dto.SupportedFunctions.HasFlag(ServiceFunctions.DoesNotNeedSetup))
                //we reset any secondary data as we expect the view to be reshown with the errors
                dto.SetupSecondaryData(_db, dto);
            return result;

        }

        /// <summary>
        /// This is available to reset any secondary data in the dto. Call this if the ModelState was invalid and
        /// you need to display the view again with errors
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        public TDto ResetDto(TDto dto)
        {
            if (!dto.SupportedFunctions.HasFlag(ServiceFunctions.DoesNotNeedSetup))
                //we reset any secondary data as we expect the view to be reshown with the errors
                dto.SetupSecondaryData(_db, dto);

            return dto;
        }

    }
}