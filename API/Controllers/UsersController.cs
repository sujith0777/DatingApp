using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IphotoService _photoService;

        public UsersController(IUserRepository userRepository, IMapper mapper, IphotoService photoService)
        {
            _photoService = photoService;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery]UserParams userParams)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            userParams.CurrentUsername = user.UserName;

            if(string.IsNullOrEmpty(userParams.Gender))
            {
                userParams.Gender = user.Gender == "male"? "female":"male";
            }

            var users = await _userRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        // api/users/3
        [HttpGet("{username}", Name="GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            _mapper.Map(memberUpdateDto, user);
            _userRepository.update(user);

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to Update User");
        }

        [HttpPost("add-photo")]

        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var result = await _photoService.AddPhotoAsync(file);

            if(result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if(user.Photos.Count == 0){
                photo.isMain = true;
            }
            user.Photos.Add(photo);
            
            if(await _userRepository.SaveAllAsync()){
                // return _mapper.Map<PhotoDto>(photo);
                return CreatedAtRoute("GetUser", new {username = user.UserName} , _mapper.Map<PhotoDto>(photo));
            }
                

            return BadRequest("Problem Adding Photo");
        }

        [HttpPut("set-main-photo/{PhotoId}")]
        public async Task<ActionResult> SetMainPhoto(int PhotoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var photo = user.Photos.FirstOrDefault(x=>x.Id == PhotoId);

            if(photo.isMain) return BadRequest("This is already ur main photo");

            var currentMain = user.Photos.FirstOrDefault(x=>x.isMain);

            if(currentMain!=null) currentMain.isMain = false;
            photo.isMain = true;

            if(await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to set main photo");
        }

        [HttpDelete("delete-photo/{photoId}")]

        public async Task<ActionResult> DeletePhoto(int photoId){

            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
             var photo = user.Photos.FirstOrDefault(x=>x.Id == photoId);
             if(photo==null) return NotFound();
             if(photo.isMain) return BadRequest("This is already ur main photo");
             if(photo.PublicId != null){
                 var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                 if(result.Error!=null) return BadRequest(result.Error.Message);
             } 
 
             user.Photos.Remove(photo);

             if(await _userRepository.SaveAllAsync()) return Ok();

             return BadRequest("Faoled to delete Photo");
        }



    }
}