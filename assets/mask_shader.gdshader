shader_type canvas_item;

uniform float slice_x : hint_range(0, 200) = 0.0;
uniform float slice_y = 0.0;
uniform float slice_width = 12;
uniform float slice_height = 28;

uniform float region_width = 290;
uniform float region_height = 28;

void fragment() {
	float pixel_x = UV.x * region_width;
	float pixel_y = UV.x * region_height;
	
	if (pixel_x < slice_x || pixel_x > slice_x + slice_width ||
        pixel_y < slice_y || pixel_y > slice_y + slice_height) {
        discard;
    }
	
	COLOR = texture(TEXTURE, UV);
}