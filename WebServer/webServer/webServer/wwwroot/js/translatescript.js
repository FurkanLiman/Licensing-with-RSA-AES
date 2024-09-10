
$(document).ready(function() {

    var arrLang={
        
        'tr':{

            '0':'Anasayfa',
            '1':'Lisans Planları',
            '2':'Hakkımızda',
            '3':'Profil',
            

        },


        'en':{
            '0':'Home',
            '1':'Our Licenses',
            '2':'AboutUs',
            '3':'Profile',
        
        },
        
        
    };


    
    function myFunction(id){
    localStorage.setItem('dil', JSON.stringify(id); 
    location.reload();
  }

    var lang =JSON.parse(localStorage.getItem('dil'));

    if(lang=="en"){
        $("#drop_yazı").html("English");
    }
    else{
        $("#drop_yazı").html("Türkçe");

    }

    $('a,h5,p,h1,h2,span,li,button,h3,label').each(function(index,element) {
      $(this).text(arrLang[lang][$(this).attr('key')]);
    
  });

});
   