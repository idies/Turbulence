bcp "select substring(data, 25, 6144) as data from turbdb201test.dbo.vel where timestep=10250 order by timestep, zindex" queryout "output2.bin" -S sciserver02 -T -f bcp.fmt
