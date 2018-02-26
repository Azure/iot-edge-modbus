#include <errno.h>
#include <fcntl.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <termios.h>
#include <unistd.h>

// speed: 4800, 9600, 19200
// char_bits: 5, 6, 7
// parity_bit: 0(none), 1(odd), 2(even)
// stop_bit: 0(none), 1(one), 2(two)

int com_set_interface_attribs(int fd, int speed, int data_bits, int parity_bit, int stop_bit)
{
	struct termios tty;

	if (tcgetattr(fd, &tty) < 0) {
		printf("Error from tcgetattr: %s\n", strerror(errno));
		return -1;
	}

	switch (speed)
	{
	case 110: {
		cfsetspeed(&tty, (speed_t)B110);
		break;
	}
	case 300: {
		cfsetspeed(&tty, (speed_t)B300);
		break;
	}
	case 600: {
		cfsetspeed(&tty, (speed_t)B600);
		break;
	}
	case 1200: {
		cfsetspeed(&tty, (speed_t)B1200);
		break;
	}
	case 2400: {
		cfsetspeed(&tty, (speed_t)B2400);
		break;
	}
	case 4800: {
		cfsetspeed(&tty, (speed_t)B4800);
		break;
	}
	case 9600: {
		cfsetspeed(&tty, (speed_t)B9600);
		break;
	}
	case 19200: {
		cfsetspeed(&tty, (speed_t)B19200);
		break;
	}
	case 38400:	{
		cfsetspeed(&tty, (speed_t)B38400);
		break;
	}
	default:
		break;
	}

	tty.c_cflag |= (CLOCAL | CREAD);    /* ignore modem controls */

	tty.c_cflag &= ~CSIZE;
	switch (data_bits)
	{
	case 5: {
		tty.c_cflag |= CS5;         /* 5-bit characters */
		break;
	}
	case 6: {
		tty.c_cflag |= CS6;         /* 6-bit characters */
		break;
	}
	case 7: {
		tty.c_cflag |= CS7;         /* 7-bit characters */
		break;
	}
	case 8: {
		tty.c_cflag |= CS8;         /* 8-bit characters */
		break;
	}
	default:
		break;
	}

	switch (parity_bit)
	{
	case 0:
		{
			tty.c_cflag &= ~PARENB; /* no parity */
			break;
		}
	case 1:
		{
			tty.c_cflag |= PARENB;
			tty.c_cflag |= PARODD;
			break;
		}
	case 2:
		{
			tty.c_cflag |= PARENB;
			tty.c_cflag &= ~PARODD;
			break;
		}
	default:
		break;
	}

	switch (stop_bit) 
	{
	case 1:
	    {
			tty.c_cflag &= ~CSTOPB; /* 1 stop bit */
			break;
		}
	case 2:
		{
			tty.c_cflag |= CSTOPB; /* 2 stop bit */
			break;
		}
	default:
		break;
	}

	tty.c_cflag &= ~CRTSCTS;    /* no hardware flowcontrol */
	tty.c_iflag &= ~(IXON | IXOFF | IXANY);    /* disable software flow control */
	tty.c_oflag &= ~OPOST;
	tty.c_cc[VTIME] = 0;
	tty.c_cc[VMIN] = 0;

#if 0
								/* setup for non-canonical mode */
	tty.c_iflag &= ~(IGNBRK | BRKINT | PARMRK | ISTRIP | INLCR | IGNCR | ICRNL | IXON);
	tty.c_lflag &= ~(ECHO | ECHONL | ICANON | ISIG | IEXTEN);
	tty.c_oflag &= ~OPOST;

	/* fetch bytes as they become available */
	tty.c_cc[VMIN] = 1;
	tty.c_cc[VTIME] = 1;
#endif

	if (tcsetattr(fd, TCSANOW, &tty) != 0) {
		printf("Error from tcsetattr: %s\n", strerror(errno));
		return -1;
	}
	return 0;
}

int com_open(const char* pathname)
{
	return open(pathname, O_RDWR | O_NOCTTY | O_NDELAY | O_EXCL);
}

int com_close(int fd)
{
	return close(fd);
}

ssize_t com_read(int fd, void *buf, size_t count)
{
	return read(fd, buf, count);
}

ssize_t com_write(int fd, const void *buf, size_t count)
{
	return write(fd, buf, count);
}

int com_tciflush(int fd)
{
	return tcflush(fd, TCIFLUSH);
}

int com_tcoflush(int fd)
{
	return tcflush(fd, TCOFLUSH);
}