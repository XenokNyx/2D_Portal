shader_type canvas_item;

uniform sampler2D noise;
uniform float dissolveVal : hint_range(0, 1);

uniform vec4 borderColor : source_color = vec4(1.0);
uniform float borderSize : hint_range(0, .5) = .1;

void fragment() {
	vec4 albedo = texture(TEXTURE, UV);
    vec4 noiseTexture = texture(noise, UV);
	
	float totalVal =  dissolveVal * (1.0 + borderSize);
		
	float val = (1.0-totalVal) + noiseTexture.x;
		
	if(val < 1.0 - borderSize){
		albedo.a = 0.0;
	}else if(val < 1.0 ){
		albedo.rgb = borderColor.rgb;
	}
	
    COLOR = albedo;
}
