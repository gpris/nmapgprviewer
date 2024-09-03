# nmapgprviewer
Naver static map and WPF GPR layer viewer

![실행화면](https://github.com/user-attachments/assets/c8a3db7e-8ccb-4a66-ba05-92d91ff0cea5)

Simple objectives are : 
1. to show Naver static map on WPF.
2. to get latitude range and longtitude range of image width and height.
3. to calculate x, y for specific lng and lat

Special thanks to:     
https://gist.github.com/pianosnake/b4a45ef6bdf2ffb2e1b44bbcca107298    
for objective #2.    

With small modifications,     
I can get bound box of naver static map image.     
as below:    

![dlng사이즈에 따른 이미지](https://github.com/user-attachments/assets/21ed3c5d-3481-449f-b19e-f8baf27b650b)

![dlat사이즈에 따른 이미지](https://github.com/user-attachments/assets/1097dadc-0fe0-44b2-bea3-38db3f54de1b)

latLngToPos API to convert lat/lng to x,y of static map image file.
A green marker of lat:37.564885 lng:126.978398  is drawn to map image position x:549, y:573  
![스크린샷 2024-07-27 150750](https://github.com/user-attachments/assets/f200b2d4-6c1f-4bbc-88fb-07daec5f1ed3)
