#ifdef __cplusplus
extern "C" {
#endif

int com_set_interface_attribs(int fd, int speed, int data_bits, int parity_bit, int stop_bit);
int com_open(const char* pathname);
int com_close(int fd);
ssize_t com_read(int fd, void *buf, size_t count);
ssize_t com_write(int fd, const void *buf, size_t count);
int com_tciflush(int fd);
int com_tcoflush(int fd);

#ifdef __cplusplus
}
#endif
